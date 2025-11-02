#!/usr/bin/env python3
# ---------------------------------------------------------
# SenseGlove Assistant v3.7 — Groq-Powered Code Generator
# ---------------------------------------------------------
# ✨ New Features:
# 1. Uses Groq API (Llama 3.3 8B) for natural language + code generation
# 2. Generate Unity C# or Python scripts from chat
# 3. Save generated code via button
# 4. All prior GUI + GitHub + watermark features retained
# ---------------------------------------------------------

import os
import json
import re
import requests
import threading
import tkinter as tk
from tkinter import PhotoImage, Canvas, Scrollbar, filedialog, messagebox

import github_updater  # local module

# ---------- CONFIG ----------
INDEX_FILE = "senseglove_index_with_functions.json"
GROQ_MODEL = "llama-3.1-8b-instant"  # Groq hosted Llama 3.3 8B
SHOW_TOP = 4
LOGO_FILE = "senseglove_logo.png"
SCRIPT_DIR = "."
GROQ_API_KEY = os.getenv("GROQ_API_KEY")

# GUI theme
BG_COLOR = "#1E1E1E"
USER_COLOR = "#3A3F44"
ASSIST_COLOR = "#2D3339"
TEXT_COLOR = "#E6E6E6"
ACCENT_COLOR = "#10A37F"


# ---------- HELPER: LOAD INDEX ----------
def load_index():
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
            fixed.append({"name": entry, "description": "Auto-added", "functions": []})
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


# ---------- HELPER: GROQ API ----------
def groq_llm(prompt: str) -> str:
    """Query Groq API (Llama 3.3 8B)"""
    if not GROQ_API_KEY:
        return "⚠️ GROQ_API_KEY not found. Please set it in your environment."

    url = "https://api.groq.com/openai/v1/chat/completions"
    headers = {"Authorization": f"Bearer {GROQ_API_KEY}", "Content-Type": "application/json"}
    data = {
        "model": GROQ_MODEL,
        "messages": [{"role": "user", "content": prompt}],
        "max_tokens": 900,
        "temperature": 0.6,
    }
    try:
        r = requests.post(url, headers=headers, json=data, timeout=90)
        if r.status_code == 200:
            return r.json()["choices"][0]["message"]["content"].strip()
        return f"[Groq Error] {r.text}"
    except Exception as e:
        return f"[Groq Exception] {e}"


# ---------- SMART SEARCH ----------
def smart_search(query: str, data):
    if not data:
        return "No script data loaded."
    context = "\n".join(
        f"{d['name']}: {d.get('description','')}\n  Functions: {', '.join(f['name'] for f in d['functions'][:8])}"
        for d in data[:80]
    )
    examples = """
Examples:
Q: How can I make the glove vibrate?
A: SG_Haptics.cs → SendHapticCommand() — sends vibration to the glove.
Q: How to detect grab?
A: SG_Grabable.cs → OnGrab() — detects grab event.
"""
    prompt = f"""
You are a Unity developer specialized in SenseGlove SDK.

Scripts and their functions:
{context}

{examples}

User query: "{query}"

Format:
ScriptName.cs → FunctionName() — concise explanation.
List up to {SHOW_TOP} results.
"""
    return groq_llm(prompt)


# ---------- CODE GENERATOR ----------
def generate_code(query):
    """Generate only Unity C# scripts related to SenseGlove SDK and JSON handling."""
    prompt = f"""
You are a professional Unity C# developer specializing in the SenseGlove SDK.

User request: {query}

Generate ONLY Unity C# code — no Python, no GUI, no external APIs.
Guidelines:
- Always use C# (.cs) syntax valid for Unity.
- Include necessary 'using' directives (UnityEngine, SG, SGCore, SGCore.Nova, System.Collections, System.Collections.Generic, System.IO, Newtonsoft.Json).
- You may use Unity's JSONUtility or Newtonsoft.Json to load or save data.
- Always focus on SenseGlove SDK classes such as:
  SG_TrackedHand, SG_HapticGlove, SG_Grabable, SG_Interactable, SG_BasicGesture, SG_CustomWaveform, SG_ForceFeedback.
- Example tasks include handling force feedback, vibrations, flexion-based interaction, glove calibration, or reading configuration data from JSON files.
- NEVER write Python or GUI code.
- Do not include markdown formatting or explanations — return pure C# source code only.
"""
    return groq_llm(prompt)



def extract_script_names(text):
    return re.findall(r"(SG_[A-Za-z0-9_]+\.cs)", text)


# ---------- GUI ----------
class SenseGloveUI:
    def __init__(self, root):
        self.root = root
        self.root.title("SenseGlove Assistant v3.8 (Groq Powered)")
        self.root.minsize(900, 700)
        self.root.configure(bg=BG_COLOR)
        self.root.rowconfigure(1, weight=1)
        self.root.columnconfigure(0, weight=1)

        # Menu
        menubar = tk.Menu(self.root)
        menubar.add_command(label="Clear Chat", command=self.clear_chat)
        self.root.config(menu=menubar)

        # Load data
        self.data = load_index()

        # Header
        header = tk.Frame(root, bg=BG_COLOR)
        header.grid(row=0, column=0, sticky="ew", padx=10, pady=(6, 5))
        if os.path.exists(LOGO_FILE):
            try:
                self.logo = PhotoImage(file=LOGO_FILE).subsample(10, 10)
                tk.Label(header, image=self.logo, bg=BG_COLOR).pack(side=tk.LEFT, padx=(0, 8))
            except Exception:
                pass
        tk.Label(
            header, text="SenseGlove Assistant",
            fg=ACCENT_COLOR, bg=BG_COLOR,
            font=("Cambria", 18, "bold")
        ).pack(side=tk.LEFT)

        # Chat container
        self.chat_container = tk.Frame(root, bg=BG_COLOR)
        self.chat_container.grid(row=1, column=0, sticky="nsew", padx=5, pady=(0, 5))
        self.chat_container.rowconfigure(0, weight=1)
        self.chat_container.columnconfigure(0, weight=1)

        self.canvas = Canvas(self.chat_container, bg=BG_COLOR, highlightthickness=0)
        self.canvas.grid(row=0, column=0, sticky="nsew")
        scrollbar = Scrollbar(self.chat_container, command=self.canvas.yview)
        scrollbar.grid(row=0, column=1, sticky="ns")
        self.canvas.configure(yscrollcommand=scrollbar.set)

        # Watermark
        if os.path.exists(LOGO_FILE):
            try:
                wm = PhotoImage(file=LOGO_FILE).subsample(4, 4)
                self.watermark = self.canvas.create_image(450, 320, image=wm, anchor="center")
                self.canvas.lower(self.watermark)
                self.canvas.image = wm
            except Exception:
                self.watermark = None

        # Chat frame inside canvas
        self.chat_frame = tk.Frame(self.canvas, bg=BG_COLOR)
        self.canvas.create_window((0, 0), window=self.chat_frame, anchor="nw")
        self.chat_frame.bind("<Configure>", lambda e: self.canvas.configure(scrollregion=self.canvas.bbox("all")))

        # Add mouse scroll bindings
        def _on_mousewheel(event):
            if event.delta:
                self.canvas.yview_scroll(int(-1 * (event.delta / 120)), "units")
            elif event.num == 4:
                self.canvas.yview_scroll(-1, "units")
            elif event.num == 5:
                self.canvas.yview_scroll(1, "units")

        self.canvas.bind_all("<MouseWheel>", _on_mousewheel)   # Windows/macOS
        self.canvas.bind_all("<Button-4>", _on_mousewheel)     # Linux up
        self.canvas.bind_all("<Button-5>", _on_mousewheel)     # Linux down

        # Input bar
        input_bar = tk.Frame(root, bg="#2A2D32")
        input_bar.grid(row=2, column=0, sticky="ew", padx=5, pady=(4, 8))
        input_bar.columnconfigure(0, weight=1)

        self.entry = tk.Entry(
            input_bar, bg="#40444B", fg=TEXT_COLOR, insertbackground=TEXT_COLOR,
            relief="flat", font=("Cambria", 13)
        )
        self.entry.grid(row=0, column=0, sticky="ew", padx=10, pady=6)
        self.entry.bind("<Return>", lambda e: self.send_query())

        tk.Button(
            input_bar, text="Send", bg=ACCENT_COLOR, fg="white",
            font=("Cambria", 12, "bold"), activebackground="#0E8C6A",
            relief="flat", cursor="hand2",
            command=self.send_query
        ).grid(row=0, column=1, padx=10)

        # Initial message
        self.add_message(
            "assistant",
            "👋 Hello! I’m your SenseGlove SDK Assistant (Groq Powered).\nAsk about scripts or say 'generate Unity code for …'"
        )

    # ---------- Chat ----------
    def clear_chat(self):
        for w in self.chat_frame.winfo_children():
            w.destroy()
        self.add_message("assistant", "🧹 Chat cleared.")

    def add_message(self, sender, text, is_code=False):
        frame = tk.Frame(self.chat_frame, bg=BG_COLOR)
        bubble = tk.Frame(frame, bg=USER_COLOR if sender == "user" else ASSIST_COLOR, padx=14, pady=10)
        label = tk.Label(
            bubble, text=text, wraplength=850, justify="left",
            bg=bubble["bg"], fg=TEXT_COLOR, font=("Cambria", 12), anchor="w"
        )
        label.pack(anchor="w")
        if is_code:
            tk.Button(
                bubble, text="💾 Save Code", bg=ACCENT_COLOR, fg="white",
                relief="flat", cursor="hand2", font=("Cambria", 10, "bold"),
                command=lambda c=text: self.save_code(c)
            ).pack(anchor="w", pady=(6, 2))
        if sender == "assistant":
            for name in extract_script_names(text):
                tk.Button(
                    bubble, text=f"Open {name}", bg=ACCENT_COLOR, fg="white",
                    cursor="hand2", relief="flat", font=("Cambria", 10, "bold"),
                    command=lambda n=name: self.open_script(n)
                ).pack(anchor="w", pady=(4, 2))
        bubble.pack(anchor="e" if sender == "user" else "w", padx=12, pady=6)
        frame.pack(fill=tk.X)
        self.canvas.update_idletasks()
        self.canvas.yview_moveto(1)

    # ---------- File Save ----------
    def save_code(self, code_text):
        file = filedialog.asksaveasfilename(
            defaultextension=".cs",
            filetypes=[("C# files", "*.cs"), ("All files", "*.*")]
        )
        if file:
            with open(file, "w", encoding="utf-8") as f:
                f.write(code_text)
            messagebox.showinfo("Saved", f"Code exported to {file}")

    # ---------- Query ----------
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
            if query.lower().startswith("generate") or "script" in query.lower() or "code" in query.lower():
                reply = generate_code(query)
                self.add_message("assistant", reply, is_code=True)
            else:
                reply = smart_search(query, self.data)
                self.add_message("assistant", reply)
        except Exception as e:
            self.add_message("assistant", f"❌ Error: {e}")

    # ---------- Script Open ----------
    def open_script(self, script_name):
        import subprocess
        github_url = f"https://github.com/Adjuvo/SenseGlove-Unity/search?q={script_name}"
        subprocess.run(["start", github_url], shell=True)
        self.add_message("assistant", f"🌐 Opened **{script_name}** on GitHub.")


# ---------- MAIN ----------
if __name__ == "__main__":
    root = tk.Tk()
    SenseGloveUI(root)
    root.mainloop()
