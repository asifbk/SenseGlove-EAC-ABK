import os
import json
import tkinter as tk
from tkinter import simpledialog, messagebox

CONFIG_DIR = os.path.join(os.path.expanduser("~"), ".senseglove_assistant")
CONFIG_FILE = os.path.join(CONFIG_DIR, "config.json")

# Same color scheme as your main app
BG_COLOR = "#0F0F0F"
ACCENT_COLOR = "#10A37F"
TEXT_COLOR = "#E6E6E6"
FONT = ("Cambria", 12)


def ensure_config_dir():
    """Make sure the config directory exists."""
    if not os.path.exists(CONFIG_DIR):
        os.makedirs(CONFIG_DIR)


def load_config():
    """Load configuration file if available."""
    ensure_config_dir()
    if os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    return {}


def save_config(data):
    """Save configuration to file."""
    ensure_config_dir()
    with open(CONFIG_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)


def get_api_key():
    """Retrieve the GROQ_API_KEY from config or prompt the user."""
    config = load_config()
    if "GROQ_API_KEY" in config and config["GROQ_API_KEY"]:
        return config["GROQ_API_KEY"]

    # Popup to ask user
    root = tk.Tk()
    root.withdraw()

    key_popup = tk.Toplevel(bg=BG_COLOR)
    key_popup.title("SenseGlove Assistant - API Setup")
    key_popup.geometry("480x200")
    key_popup.resizable(False, False)

    tk.Label(key_popup, text="🔐 Enter your Groq API Key", bg=BG_COLOR, fg=ACCENT_COLOR,
             font=("Cambria", 14, "bold")).pack(pady=(20, 5))

    entry = tk.Entry(key_popup, width=55, bg="#40444B", fg=TEXT_COLOR,
                     insertbackground=TEXT_COLOR, relief="flat", font=FONT)
    entry.pack(pady=10)
    entry.focus()

    status_label = tk.Label(key_popup, text="", bg=BG_COLOR, fg="#A0A0A0", font=FONT)
    status_label.pack()

    def save_key():
        key = entry.get().strip()
        if not key:
            messagebox.showwarning("Missing Key", "Please enter your Groq API key.")
            return
        config["GROQ_API_KEY"] = key
        save_config(config)
        status_label.config(text="✅ Key saved successfully! You can close this window.")
        key_popup.after(1500, key_popup.destroy)

    tk.Button(key_popup, text="Save Key", bg=ACCENT_COLOR, fg="white", relief="flat",
              cursor="hand2", font=("Cambria", 12, "bold"),
              command=save_key).pack(pady=(10, 15))

    key_popup.mainloop()

    # Re-load key after popup closes
    return load_config().get("GROQ_API_KEY")
