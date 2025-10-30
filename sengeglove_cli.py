#!/usr/bin/env python3
# ---------------------------------------------------------
# SenseGlove Assistant v3.0 — Fully Stable & Self-Healing
# ---------------------------------------------------------
# ✨ Features:
# - Opens suggested scripts directly on GitHub
# - Detects and fixes broken JSON structures
# - Syncs GitHub updates automatically if new scripts exist
# - Friendly GUI with clean ChatGPT-like styling
# ---------------------------------------------------------

import os
import json
import re
import subprocess
import threading
import tkinter as tk
from tkinter import PhotoImage, Canvas, Scrollbar, messagebox

import github_updater  # local module

# ---------- CONFIG ----------
INDEX_FILE = "senseglove_index_with_functions.json"
OLLAMA_PATH = r"C:\Users\mkarim1\AppData\Local\Programs\Ollama\ollama.exe"
OLLAMA_MODEL = "llama3.2"
SHOW_TOP = 4
LOGO_FILE = "senseglove_logo.png"

SCRIPT_DIR = None
VSCODE_EXE = r"C:\Users\mkarim1\AppData\Local\Programs\Microsoft VS Code\Code.exe"

# GUI theme
BG_COLOR = "#1E1E1E"
USER_COLOR = "#3A3F44"
ASSIST_COLOR = "#2D3339"
TEXT_COLOR = "#E6E6E6"
ACCENT_COLOR = "#10A37F"


# ---------- HELPER: LOAD + FIX INDEX ----------
def load_index():
    """Load or rebuild the index; auto-fix malformed data."""
    if not os.path.exists(INDEX_FILE):
        print("⚠️ Index not found — fetching from GitHub...")
        github_updater.update_index()

    try:
        with open(INDEX_FILE, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception as e:
        print(f"❌ Failed to read JSON: {e}. Regenerating...")
        github_updater.update_index()
        with open(INDEX_FILE, "r", encoding="utf-8") as f:
            data = json.load(f)

    scripts = data.get("scripts", data) if isinstance(data, dict) else data
    fixed = []
    for entry in scripts:
        if isinstance(entry, str):
            fixed.append({"name": entry, "description": "Auto-added script entry", "functions": []})
            continue
        if not isinstance(entry, dict):
            continue
        name = entry.get("name", "Unnamed Script")
        desc = entry.get("description", "No description available.")
        funcs = entry.get("functions", [])
        if not funcs:
            funcs = [{"name": "UnknownFunction", "description": "No function details available."}]
        elif isinstance(funcs[0], str):
            funcs = [{"name": fn, "description": "Recovered function"} for fn in funcs]
        elif isinstance(funcs[0], dict) and "description" not in funcs[0]:
            for f in funcs:
                f["description"] = "Auto-fixed description."
        fixed.append({"name": name, "description": desc, "functions": funcs})

    with open(INDEX_FILE, "w", encoding="utf-8") as f:
        json.dump({"scripts": fixed}, f, indent=2)
    print(f"✅ JSON validated — {len(fixed)} scripts loaded.")
    return fixed


# ---------- HELPER: LLM ----------
def local_llm(prompt: str) -> str:
    """Run a local Ollama LLM query safely."""
    try:
        result = subprocess.run(
            [OLLAMA_PATH, "run", OLLAMA_MODEL],
            input=prompt.encode("utf-8"),
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=90,
        )
        output = result.stdout.decode("utf-8").strip()
        if not output:
            err = result.stderr.decode("utf-8").strip()
            return f"[LLM Error] {err or 'No response from model.'}"
        return output
    except Exception as e:
        return f"[LLM Error] {e}"


# ---------- SMART SEARCH ----------
def smart_search(query: str, data):
    """Use the LLM to map a query → script + function."""
    if not data:
        return "No script data loaded."

    context = "\n".join(
        f"{d['name']}: {d.get('description','')}\n"
        f"  Functions: {', '.join(f['name'] for f in d['functions'][:8])}"
        for d in data[:80]
    )

    examples = """
Examples:
Q: How can I make the glove vibrate?
A: SG_Haptics.cs → SendHapticCommand() — sends vibration to the glove.
   SG_ObjectVibration.cs → ApplyVibration() — applies vibration to grabbed objects.

Q: How to detect when an object is grabbed?
A: SG_Grabable.cs → OnGrab() — detects grab event.
   SG_PhysicsGrab.cs → AttachRigidbody() — attaches object physics to hand.

Q: How to start calibration?
A: SG_CalibrationVoid.cs → StartCalibration() — starts glove calibration process.
"""

    prompt = f"""
You are a professional Unity developer specializing in SenseGlove SDK.

Scripts and their functions:
{context}

{examples}

User query: "{query}"

List up to {SHOW_TOP} relevant scripts and functions using this format:
ScriptName.cs → FunctionName() — short explanation.
"""

    reply = local_llm(prompt)
    return reply if len(reply) > 5 else "No relevant scripts or functions found."


def extract_script_names(text):
    """Extract script filenames from model output."""
    return re.findall(r"(SG_[A-Za-z0-9_]+\.cs)", text)


# ---------- GUI CLASS ----------
class SenseGloveUI:
    def __init__(self, root):
        self.root = root
        self.root.title("SenseGlove Assistant v3.0")
        self.root.geometry("900x660")
        self.root.configure(bg=BG_COLOR)

        if os.path.exists(LOGO_FILE):
            try:
                self.root.iconphoto(False, PhotoImage(file=LOGO_FILE))
            except Exception:
                pass

        self.data = load_index()

        header = tk.Frame(root, bg=BG_COLOR)
        header.pack(fill=tk.X, pady=(6, 5), padx=10)
        if os.path.exists(LOGO_FILE):
            try:
                self.logo = PhotoImage(file=LOGO_FILE).subsample(10, 10)
                tk.Label(header, image=self.logo, bg=BG_COLOR).pack(side=tk.LEFT, padx=(0, 8))
            except Exception:
                pass
        tk.Label(header, text="SenseGlove Assistant", fg=ACCENT_COLOR,
                 bg=BG_COLOR, font=("Cambria", 18, "bold")).pack(side=tk.LEFT)

        self.chat_container = tk.Frame(root, bg=BG_COLOR)
        self.chat_container.pack(fill=tk.BOTH, expand=True)
        self.canvas = Canvas(self.chat_container, bg=BG_COLOR, highlightthickness=0)
        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar = Scrollbar(self.chat_container, command=self.canvas.yview)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.canvas.configure(yscrollcommand=scrollbar.set)
        self.chat_frame = tk.Frame(self.canvas, bg=BG_COLOR)
        self.canvas.create_window((0, 0), window=self.chat_frame, anchor="nw")
        self.chat_frame.bind("<Configure>", lambda e: self.canvas.configure(scrollregion=self.canvas.bbox("all")))

        input_bar = tk.Frame(root, bg="#2A2D32")
        input_bar.pack(fill=tk.X, pady=(6, 10))
        self.entry = tk.Entry(input_bar, bg="#40444B", fg=TEXT_COLOR,
                              insertbackground=TEXT_COLOR, relief="flat",
                              font=("Cambria", 13))
        self.entry.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=10, pady=6)
        self.entry.bind("<Return>", lambda e: self.send_query())
        tk.Button(input_bar, text="Send", bg=ACCENT_COLOR, fg="white",
                  font=("Cambria", 12, "bold"), activebackground="#0E8C6A",
                  relief="flat", cursor="hand2",
                  command=self.send_query).pack(side=tk.RIGHT, padx=10)

        self.add_message("assistant", "👋 Hello! I’m your SenseGlove SDK Assistant.\nAsk me about any script, function, or system behavior.")
        self.thinking_label = None

    def add_message(self, sender, text):
        frame = tk.Frame(self.chat_frame, bg=BG_COLOR)
        bubble = tk.Frame(frame, bg=USER_COLOR if sender == "user" else ASSIST_COLOR, padx=14, pady=10)
        label = tk.Label(bubble, text=text, wraplength=680, justify="left",
                         bg=bubble["bg"], fg=TEXT_COLOR, font=("Cambria", 12))
        label.pack(anchor="w")

        if sender == "assistant":
            for name in extract_script_names(text):
                tk.Button(bubble, text=f"Open {name}", bg=ACCENT_COLOR, fg="white",
                          cursor="hand2", relief="flat", font=("Cambria", 10, "bold"),
                          command=lambda n=name: self.open_script(n)).pack(anchor="w", pady=(4, 2))

        bubble.pack(anchor="e" if sender == "user" else "w", padx=12, pady=6)
        frame.pack(fill=tk.X)
        self.canvas.update_idletasks()
        self.canvas.yview_moveto(1)

    def send_query(self):
        query = self.entry.get().strip()
        if not query:
            return
        self.add_message("user", query)
        self.entry.delete(0, tk.END)
        threading.Thread(target=self.process_query, args=(query,), daemon=True).start()

    def process_query(self, query):
        self.add_message("assistant", "Thinking …")
        try:
            reply = smart_search(query, self.data)
        except Exception as e:
            reply = f"❌ Error: {e}"

        if self.thinking_label:
            self.thinking_label.destroy()
            self.thinking_label = None
        self.add_message("assistant", reply)

    def open_script(self, script_name):
        """Always open the script directly on GitHub."""
        try:
            github_url = f"https://github.com/Adjuvo/SenseGlove-Unity/search?q={script_name}"
            subprocess.run(["start", github_url], shell=True)
            self.add_message("assistant", f"🌐 Opened **{script_name}** on GitHub — showing latest version online.")
        except Exception as e:
            self.add_message("assistant", f"❌ Could not open GitHub link: {e}")


# ---------- MAIN ----------
if __name__ == "__main__":
    root = tk.Tk()
    SenseGloveUI(root)
    root.mainloop()
