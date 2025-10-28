#!/usr/bin/env python3
# ---------------------------------------------------------
# SenseGlove Assistant – Function-aware ChatGPT-style GUI
# ---------------------------------------------------------
# ✨ Features:
# - Uses senseglove_index_with_functions.json (includes functions)
# - Few-shot examples to teach Ollama how to answer
# - Returns Script → Function → Explanation
# - Opens files in VS Code using full path
# ---------------------------------------------------------

import tkinter as tk
from tkinter import PhotoImage, Canvas, Scrollbar, messagebox
import subprocess, json, os, re, threading

# ---------- Configuration ----------
INDEX_FILE   = "senseglove_index_with_functions.json"
OLLAMA_MODEL = "llama3.2"
SHOW_TOP     = 4
LOGO_FILE    = "senseglove_logo.png"

SCRIPT_DIR   = r"C:\Users\mkarim1\Desktop\My Unity Projects\SenseGlove\SenseGlove-Unity-master\SenseGlove-Unity-master\SenseGlove\Scripts"
VSCODE_EXE   = r"C:\Users\mkarim1\AppData\Local\Programs\Microsoft VS Code\Code.exe"

# --- Theme colors ---
BG_COLOR     = "#1E1E1E"
USER_COLOR   = "#3A3F44"
ASSIST_COLOR = "#2D3339"
TEXT_COLOR   = "#E6E6E6"
ACCENT_COLOR = "#10A37F"
# -----------------------------------

def load_index():
    if not os.path.exists(INDEX_FILE):
        raise FileNotFoundError(f"{INDEX_FILE} not found.")
    with open(INDEX_FILE, "r", encoding="utf-8") as f:
        return json.load(f)

def local_llm(prompt: str) -> str:
    """Run the query through local Ollama model"""
    try:
        result = subprocess.run(
            ["ollama", "run", OLLAMA_MODEL],
            input=prompt.encode("utf-8"),
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            timeout=90
        )
        return result.stdout.decode("utf-8").strip()
    except Exception as e:
        return f"[LLM Error] {e}"

def smart_search(query: str, data):
    """
    Ask the LLM which script and function are relevant to the query.
    Includes few-shot examples and function lists for better reasoning.
    """
    # Build detailed context from JSON
    context_lines = []
    for i, d in enumerate(data):
        funcs = d.get("functions", [])
        func_text = ", ".join(f["name"] for f in funcs[:10]) if funcs else "No functions listed"
        context_lines.append(
            f"{i+1}. {d['name']}:\n   Description: {d['description']}\n   Functions: {func_text}\n"
        )
    context = "\n".join(context_lines)

    # Few-shot examples to guide the model
    examples = """
Examples:
Q: Which script handles vibration or haptic feedback?
A: SG_ObjectVibration.cs → ApplyVibration() — applies vibration feedback to grabbed objects.
   SG_Haptics.cs → SendHapticCommand() — sends haptic pattern data to the glove.

Q: Where is calibration handled?
A: SG_CalibrationVoid.cs → StartCalibration() — begins glove calibration routine.

Q: Which function computes force feedback?
A: SG_ImpactFeedback.cs → ComputeForceFeedback() — calculates impact-based force level.
   SG_FingerFeedback.cs → CalculateFingerForce() — adjusts force per finger.
"""

    # Build the full LLM prompt
    prompt = f"""
You are a senior Unity developer specializing in SenseGlove SDK.
Below is a knowledge base of all scripts and their key functions.

{context}

{examples}

Now answer the following user question:

Question: "{query}"

Please list up to {SHOW_TOP} relevant scripts and functions.
Use the following format:
ScriptName.cs → FunctionName() — short explanation.

If you cannot find an exact match, infer from similar function names.
Always prefer function names actually appearing in the list.
"""

    reply = local_llm(prompt)
    return reply if len(reply) > 5 else "No relevant scripts or functions found."

def extract_script_names(text):
    return re.findall(r"(SG_[A-Za-z0-9_]+\.cs)", text)

# ---------- GUI ----------
class ChatGPTLikeUI:
    def __init__(self, root):
        self.root = root
        self.root.title("SenseGlove Assistant")
        self.root.geometry("880x640")
        self.root.configure(bg=BG_COLOR)

        # Icon
        if os.path.exists(LOGO_FILE):
            try:
                self.root.iconphoto(False, PhotoImage(file=LOGO_FILE))
            except Exception:
                pass

        # Load data
        try:
            self.data = load_index()
        except FileNotFoundError as e:
            messagebox.showerror("Error", str(e))
            root.destroy()
            return

        # Header
        header = tk.Frame(root, bg=BG_COLOR)
        header.pack(fill=tk.X, pady=(6, 5), padx=10)

        if os.path.exists(LOGO_FILE):
            try:
                self.logo = PhotoImage(file=LOGO_FILE).subsample(10, 10)
                tk.Label(header, image=self.logo, bg=BG_COLOR).pack(side=tk.LEFT, padx=(0, 8))
            except Exception:
                pass

        tk.Label(header, text="SenseGlove Assistant",
                 fg=ACCENT_COLOR, bg=BG_COLOR, font=("Cambria", 18, "bold")).pack(side=tk.LEFT)

        # Chat area
        self.chat_container = tk.Frame(root, bg=BG_COLOR)
        self.chat_container.pack(fill=tk.BOTH, expand=True)

        self.canvas = Canvas(self.chat_container, bg=BG_COLOR, highlightthickness=0)
        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        scrollbar = Scrollbar(self.chat_container, command=self.canvas.yview)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.canvas.configure(yscrollcommand=scrollbar.set)

        self.chat_frame = tk.Frame(self.canvas, bg=BG_COLOR)
        self.canvas.create_window((0, 0), window=self.chat_frame, anchor="nw")
        self.chat_frame.bind("<Configure>",
                             lambda e: self.canvas.configure(scrollregion=self.canvas.bbox("all")))

        # Input bar
        input_bar = tk.Frame(root, bg="#2A2D32")
        input_bar.pack(fill=tk.X, pady=(6, 10))

        self.entry = tk.Entry(input_bar, bg="#40444B", fg=TEXT_COLOR,
                              insertbackground=TEXT_COLOR, relief="flat",
                              font=("Cambria", 13))
        self.entry.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=10, pady=6)
        self.entry.bind("<Return>", lambda e: self.send_query())

        tk.Button(input_bar, text="Send", bg=ACCENT_COLOR, fg="white",
                  font=("Cambria", 12, "bold"), activebackground="#0E8C6A",
                  relief="flat", cursor="hand2", command=self.send_query).pack(side=tk.RIGHT, padx=10)

        # Greeting
        self.add_message("assistant", "Hello 👋 I’m your SenseGlove SDK Assistant.\nAsk about any script or function!")

        self.thinking_label = None

    # ---------- Chat bubbles ----------
    def add_message(self, sender, text):
        bubble_frame = tk.Frame(self.chat_frame, bg=BG_COLOR)
        bubble = tk.Frame(bubble_frame,
                          bg=USER_COLOR if sender == "user" else ASSIST_COLOR,
                          padx=14, pady=10)
        label = tk.Label(bubble, text=text, wraplength=640, justify="left",
                         bg=bubble["bg"], fg=TEXT_COLOR, font=("Cambria", 12))
        label.pack(anchor="w")

        if sender == "assistant":
            for name in extract_script_names(text):
                tk.Button(bubble, text=f"Open {name}", bg=ACCENT_COLOR, fg="white",
                          cursor="hand2", relief="flat",
                          font=("Cambria", 10, "bold"),
                          command=lambda n=name: self.open_in_vscode(n)).pack(anchor="w", pady=(4, 2))

        bubble.pack(anchor="e" if sender == "user" else "w", padx=12, pady=6)
        bubble_frame.pack(fill=tk.X)
        self.canvas.update_idletasks()
        self.canvas.yview_moveto(1)
        if text.startswith("Thinking"):
            self.thinking_label = bubble_frame

    # ---------- Message flow ----------
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
            reply = f"Error: {e}"
        if self.thinking_label:
            self.thinking_label.destroy()
            self.thinking_label = None
        self.add_message("assistant", reply)

    # ---------- Open scripts ----------
    def open_in_vscode(self, script_name):
        """Open the script in VS Code and show confirmation"""
        try:
            full_path = os.path.join(SCRIPT_DIR, script_name)
            if os.path.exists(full_path):
                subprocess.run([VSCODE_EXE, full_path])
                self.add_message("assistant", f"✅ Opened {script_name} in VS Code.")
                return
            for root, _, files in os.walk(SCRIPT_DIR):
                if script_name in files:
                    full_path = os.path.join(root, script_name)
                    subprocess.run([VSCODE_EXE, full_path])
                    self.add_message("assistant", f"✅ Opened {script_name} in VS Code.")
                    return
            self.add_message("assistant", f"⚠️ Could not find {script_name}.")
        except Exception as e:
            self.add_message("assistant", f"❌ Error opening {script_name}: {e}")

# ---------- Run ----------
if __name__ == "__main__":
    root = tk.Tk()
    ChatGPTLikeUI(root)
    root.mainloop()
