#!/usr/bin/env python3
# ---------------------------------------------------------
# generate_full_index.py
# ---------------------------------------------------------
# Fetches all .cs scripts from the official SenseGlove-Unity repo,
# parses classes and functions, and builds a rich JSON index
# compatible with your GUI assistant.
# ---------------------------------------------------------

import os, re, json, base64, requests
from datetime import datetime

REPO = "Adjuvo/SenseGlove-Unity"
BRANCH = "master"
OUT_FILE = "senseglove_index_with_functions.json"
HEADERS = {}

# Optional: read your GitHub token for higher rate limit (recommended)
if os.getenv("GITHUB_TOKEN"):
    HEADERS = {"Authorization": f"token {os.getenv('GITHUB_TOKEN')}"}
else:
    print("⚠️  No GitHub token detected (limit = 60 requests/hour).")
    print("    Set one with:")
    print('    setx GITHUB_TOKEN "your_token_here" (Windows)')
    print('    export GITHUB_TOKEN="your_token_here" (Linux/Mac)')

API_BASE = f"https://api.github.com/repos/{REPO}"


# ---------- GitHub helpers ----------

def get_repo_tree(branch=BRANCH):
    """Return all files in the repo tree."""
    url = f"{API_BASE}/git/trees/{branch}?recursive=1"
    r = requests.get(url, headers=HEADERS)
    r.raise_for_status()
    tree = [i for i in r.json()["tree"] if i["path"].endswith(".cs")]
    return tree


def get_file_content(path):
    """Download raw C# content from GitHub."""
    url = f"{API_BASE}/contents/{path}"
    r = requests.get(url, headers=HEADERS)
    r.raise_for_status()
    data = r.json()
    return base64.b64decode(data["content"]).decode("utf-8", errors="ignore")


# ---------- C# parsing logic ----------

def extract_classes(code):
    """Extract class names from C#."""
    return re.findall(r"class\s+([A-Za-z0-9_]+)", code)


def extract_functions(code):
    """Extract function names and generate summaries."""
    pattern = re.compile(
        r"(?:public|private|protected|internal)?\s*(?:static\s+)?(?:void|int|float|bool|string|\w+)\s+([A-Za-z0-9_]+)\s*\("
    )
    functions = pattern.findall(code)
    results = []
    for f in functions:
        desc = (
            "Handles logic for " + f.lower().replace("_", " ")
            if len(f) > 3
            else "Utility method."
        )
        if "update" in f.lower():
            desc = "Called every frame to update states or visuals."
        elif "start" in f.lower():
            desc = "Runs once at initialization."
        elif "oncollision" in f.lower():
            desc = "Responds to object collision events."
        elif "grab" in f.lower():
            desc = "Controls object grabbing or releasing."
        elif "vibrate" in f.lower() or "haptic" in f.lower():
            desc = "Controls haptic vibration feedback on the glove."
        results.append({"name": f, "description": desc})
    return results


def guess_summary(name, classes, functions):
    """Rough AI-style auto-summary."""
    base = name.replace(".cs", "")
    if "Grab" in base:
        return "Manages object grabbing, releasing, or physics interactions."
    if "Hand" in base:
        return "Handles tracking, animation, or feedback for SenseGlove hands."
    if "XR" in base:
        return "Integrates SenseGlove with Unity XR tracking and controllers."
    if "Material" in base:
        return "Defines or detects materials for haptic responses."
    if "Waveform" in base or "Haptic" in base:
        return "Defines vibration or haptic signal patterns for feedback."
    if "Core" in base:
        return "Provides SDK initialization and core system management."
    return f"Auto-generated description for {base} script."


# ---------- Main builder ----------

def generate_index():
    print("🔍 Fetching C# scripts from GitHub...")
    tree = get_repo_tree()
    total = len(tree)
    scripts = []

    for i, item in enumerate(tree, start=1):
        name = os.path.basename(item["path"])
        print(f"   [{i}/{total}] Parsing {name}")
        try:
            code = get_file_content(item["path"])
            classes = extract_classes(code)
            functions = extract_functions(code)
            summary = guess_summary(name, classes, functions)
            entry = {
                "script_name": name,
                "summary": summary,
                "tags": list(set([tag.lower() for tag in re.findall(r"[A-Z][a-z]+", name)])),
                "classes": classes,
                "functions": functions,
                "last_updated": datetime.utcnow().isoformat() + "Z",
            }
            scripts.append(entry)
        except Exception as e:
            print(f"⚠️  Skipping {name}: {e}")

    final_data = {"total_scripts": len(scripts), "scripts": scripts}

    with open(OUT_FILE, "w", encoding="utf-8") as f:
        json.dump(final_data, f, indent=2, ensure_ascii=False)

    print(f"\n✅ JSON index generated: {OUT_FILE}")
    print(f"📄 Total scripts parsed: {len(scripts)}")


# ---------- Run ----------
if __name__ == "__main__":
    try:
        generate_index()
    except Exception as e:
        print("❌ Error:", e)
