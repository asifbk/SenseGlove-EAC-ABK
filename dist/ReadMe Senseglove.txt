📘 README.md (put this inside your folder)

Here’s a ready-to-use professional README for your GitHub or ZIP release 👇

🧠 SenseGlove Assistant (Desktop App)

SenseGlove Assistant is an AI-powered desktop application built with Python and the Groq API.
It allows users to explore, understand, and generate Unity C# code based on the official SenseGlove SDK.
It works as a smart developer companion — perfect for VR/haptics researchers and Unity developers.

🚀 Features

💬 Ask anything about the SenseGlove Unity SDK

🔍 Explore functions (e.g. OnGrab(), SendHapticCommand())

🧩 Click to open official scripts directly on GitHub

⚙️ Auto-generate C# code for Unity (no Python or web knowledge needed)

🌐 Uses Groq’s Llama-3.1-8B-Instant model for local-like fast answers

🎨 Clean, dark, ChatGPT-style GUI made in Tkinter

🧭 How to Use
🪶 Option 1 — Ready-to-Run EXE (Recommended)

Download and extract the ZIP.

Open the folder SenseGlove-Assistant/dist/.

Double-click SenseGlove_Assistant.exe 🚀
(Windows SmartScreen may warn once; click “More info → Run anyway”)

That’s it — no setup required!

🧩 Option 2 — Run from Source (Developers)

If you prefer running the Python version:

Install Python 3.9+ (tested on 3.13.5)

Install dependencies:

pip install requests tkinter pyinstaller


Set your Groq API key in the environment:

setx GROQ_API_KEY "gsk_XXXXXXXXXXXXXXXXXXXXXXXX"


Run:

python senseglove_cli.py

🔑 Requirements

Windows 10/11 (64-bit)

Internet connection (for Groq API and GitHub)

GROQ API key (get free key from https://console.groq.com
)

🧰 Developer Notes

If you wish to rebuild the EXE:

.\build_app.bat


The new executable will appear inside the dist folder.

🧾 Credits

Developed by Asif Bin Karim (UALR, VR/Haptics Research)
Built using:

Python 3.13.5

Tkinter

Groq API (Llama-3.1-8B)

SenseGlove Unity SDK (by Adjuvo)

🛡️ License

This project is for research and educational use only.
Not affiliated with SenseGlove BV or Adjuvo.

📦 ZIP Packaging Checklist ✅
File/Folder	Purpose	Include in ZIP?
dist/SenseGlove_Assistant.exe	Main executable	✅
senseglove_index_with_functions.json	Script index	✅
senseglove_logo.png	Logo asset	✅
github_updater.py	GitHub fetch helper	✅
README.md	Instructions	✅
build_app.bat	Optional developer build	✅
LICENSE	Optional legal file	✅
🧠 Next Upgrade (Optional Ideas)

Add automatic update checker via GitHub API

Bundle GROQ_API_KEY prompt window on first launch

Add “Dark/Light” theme switcher

Convert to .msi or .exe installer (using Inno Setup or NSIS)