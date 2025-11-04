#!/usr/bin/env python3
# ---------------------------------------------------------
# SenseGlove GitHub Updater (Fixed + Structured v2.2)
# ---------------------------------------------------------
# ✅ Key Improvements:
# - Keeps full "functions" structure: [{"name": ..., "description": ...}]
# - Smart auto-descriptions for Start, Update, Grab, Vibrate, etc.
# - Skips empty or malformed files
# - Merges safely with existing JSON (never wipes valid data)
# ---------------------------------------------------------

import os
import re
import json
import base64
import requests
from datetime import datetime

REPO = "Adjuvo/SenseGlove-Unity"
BRANCH = "master"
INDEX_FILE = "senseglove_index_with_functions.json"

# ---------- Helper: GitHub Auth ----------
def get_headers():
    token = os.getenv("GITHUB_TOKEN")
    if not token:
        print("⚠️  Warning: GITHUB_TOKEN not set. You may hit rate limits.")
        return {}
    return {"Authorization": f"token {token}"}

# ---------- Helper: Fetch repo structure ----------
def fetch_repo_tree():
    url = f"https://api.github.com/repos/{REPO}/git/trees/{BRANCH}?recursive=1"
    r = requests.get(url, headers=get_headers())
    r.raise_for_status()
    tree = r.json().get("tree", [])
    return [t for t in tree if t["path"].endswith(".cs")]

# ---------- Helper: Fetch C# content ----------
def fetch_cs_content(path):
    url = f"https://api.github.com/repos/{REPO}/contents/{path}"
    r = requests.get(url, headers=get_headers())
    if r.status_code != 200:
        print(f"⚠️  Skipping {path}: HTTP {r.status_code}")
        return ""
    data = r.json()
    return base64.b64decode(data["content"]).decode("utf-8", errors="ignore")

# ---------- Helper: Parse C# for classes & functions ----------
def parse_csharp(content):
    class_pattern = re.compile(r"class\s+([A-Za-z0-9_]+)")
    func_pattern = re.compile(
        r"(?:public|private|protected|internal)?\s*(?:static\s+)?"
        r"(?:void|int|float|bool|string|\w+)\s+([A-Za-z0-9_]+)\s*\("
    )
    classes = class_pattern.findall(content)
    funcs = func_pattern.findall(content)
    return classes, funcs

# ---------- Helper: Build detailed structured entry ----------
def build_entry(name, content):
    classes, funcs = parse_csharp(content)
    functions = []

    for f in funcs:
        desc = "Handles logic for " + f.lower().replace("_", " ")
        if "update" in f.lower():
            desc = "Called every frame to update states or visuals."
        elif "start" in f.lower():
            desc = "Runs once at initialization."
        elif "grab" in f.lower():
            desc = "Controls object grabbing or releasing."
        elif "vibrate" in f.lower() or "haptic" in f.lower():
            desc = "Controls vibration or haptic feedback."
        elif "calibration" in f.lower():
            desc = "Handles glove calibration logic."
        elif "collision" in f.lower() or "impact" in f.lower():
            desc = "Processes impact or collision feedback."
        elif "render" in f.lower() or "display" in f.lower():
            desc = "Manages rendering or display logic."
        elif "force" in f.lower():
            desc = "Computes or applies force feedback."
        functions.append({"name": f, "description": desc})

    return {
        "name": name,
        "description": f"Auto-generated entry for {name}. Found {len(classes)} class(es) and {len(functions)} function(s).",
        "classes": classes,
        "functions": functions,
        "tags": [],
        "last_updated": datetime.utcnow().isoformat() + "Z"
    }

# ---------- Update index ----------
def update_index():
    try:
        # Load existing data
        if os.path.exists(INDEX_FILE):
            with open(INDEX_FILE, "r", encoding="utf-8") as f:
                local_data = json.load(f)
            scripts = local_data.get("scripts", local_data) if isinstance(local_data, dict) else local_data
        else:
            scripts = []

        print(f"🔍 Checking GitHub repo ({REPO}) for new .cs scripts…")
        remote_files = fetch_repo_tree()
        existing_names = {s.get("name", s.get("script_name")) for s in scripts}

        added = 0
        for item in remote_files:
            name = os.path.basename(item["path"])
            if name not in existing_names:
                print(f"➕ Adding new script: {name}")
                code = fetch_cs_content(item["path"])
                if not code.strip():
                    print(f"⚠️  Skipping {name} (empty or unreadable)")
                    continue
                entry = build_entry(name, code)
                scripts.append(entry)
                added += 1

        if added > 0:
            print(f"✅ Added {added} new scripts. Writing updated index…")
            with open(INDEX_FILE, "w", encoding="utf-8") as f:
                json.dump({"total_scripts": len(scripts), "scripts": scripts}, f, indent=2)
            print("💾 Index successfully updated and saved.")
        else:
            print("✅ No new scripts found. Index is already up to date.")

    except requests.exceptions.RequestException as e:
        print(f"❌ Network error: {e}")
    except Exception as e:
        print(f"⚠️ Unexpected error during update: {e}")

# ---------- Main run ----------
if __name__ == "__main__":
    update_index()
