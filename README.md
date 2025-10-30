A friendly, local assistant that helps you find the right SenseGlove Unity script & function for any task, and opens the latest source on GitHub with one click.

✨ What it does

Understands your question (e.g., “my finger clips through objects”) and maps it to scripts/functions (e.g., SG_Grabable.cs → OnGrab()).

Shows relevant scripts in a GUI, with “Open on GitHub” buttons.

Keeps its knowledge fresh by updating an index file (senseglove_index_with_functions.json) from the official repo.

Self-heals: repairs malformed JSON, handles missing descriptions, and loads even if some entries are incomplete.

🧱 Project Files

sengeglove_cli.py — GUI assistant.

github_updater.py — Fetches scripts/functions from GitHub and builds/updates the JSON index.

senseglove_index_with_functions.json — Local knowledge base (scripts + functions).

(Optional) generate_full_index.py — A richer offline generator if you have a local clone.

🔧 Prerequisites

Python 3.9+ (Windows)

Ollama (local LLM runner)

Download & install: https://ollama.com/

Make sure models you want exist locally (e.g., llama3.2):

ollama list
ollama pull llama3.2


GitHub Personal Access Token (PAT) (recommended)

Needed to avoid GitHub rate limits and to fetch reliably.

Create a token (no special scopes needed for public repos).

Set it on Windows PowerShell:

setx GITHUB_TOKEN "ghp_yourTokenHere"


Close & reopen the terminal so the env var is available.

⚙️ Configuration (defaults in sengeglove_cli.py)
INDEX_FILE   = "senseglove_index_with_functions.json"
OLLAMA_PATH  = r"C:\Users\<you>\AppData\Local\Programs\Ollama\ollama.exe"
OLLAMA_MODEL = "llama3.2"
LOGO_FILE    = "senseglove_logo.png"

# GitHub-only open mode (no local repo needed)
SCRIPT_DIR   = None


GitHub-only mode is already enabled (SCRIPT_DIR = None).
Clicking a script opens its GitHub search page (latest code).

🚀 Run the Assistant
python sengeglove_cli.py


You’ll see a dark UI:

Type your question (“how do I vibrate on impact?”).

Assistant replies with Script → Function → One-line why.

Click “Open SG_*.cs” to open the latest code on GitHub.

🌱 How the JSON Index is created & updated

We keep a local file, senseglove_index_with_functions.json, that looks like this:

{
  "scripts": [
    {
      "name": "SG_Grabable.cs",
      "description": "Controls object grabbing and releasing.",
      "functions": [
        {"name": "OnGrab", "description": "Detects grab event."},
        {"name": "OnRelease", "description": "Handles release logic."}
      ]
    }
  ]
}

Where it comes from

On first run, sengeglove_cli.py tries to load the index.

If missing or broken, it calls github_updater.update_index():

Lists all .cs files in the official repo (Adjuvo/SenseGlove-Unity) via GitHub API.

Fetches the file contents.

Parses class names & function signatures (regex-based).

Auto-builds structured entries (with auto descriptions for common patterns like Start, Update, Grab, Haptic, Calibration, etc.).

Merges with existing data and writes back the JSON.

Why a GitHub token?

GitHub’s unauthenticated rate limit is very small. You’ll quickly hit:

403 rate limit exceeded


Setting GITHUB_TOKEN raises your limit and makes updates reliable.

If the repo ever becomes private, a token with read permission would be required.

🔄 How the assistant stays up-to-date

On startup, the assistant loads senseglove_index_with_functions.json.

If empty/malformed, it repairs fields (adds missing description, converts function strings to rich objects).

If the index is missing, it asks the updater to fetch from GitHub.

When new scripts are published in the repo, running the updater again adds them to the index.

Manual update (any time):

python github_updater.py

🧠 How the assistant answers

Builds a compact context from the JSON:
script names + short descriptions + up to 8 function names per script.

Adds a few examples (few-shot prompts) to guide the LLM.

Sends your question + context to Ollama (llama3.2 by default).

Gets back a human-friendly list like:

SG_Haptics.cs → SendHapticCommand() — triggers vibration motors.
SG_ObjectVibration.cs → ApplyVibration() — applies object-based vibration when grabbed/touched.


For each SG_*.cs referenced, the UI shows a button Open SG_*.cs → opens GitHub.

🖱️ Opening scripts (GitHub-only mode)

We always open the latest code online.

Button click → opens the GitHub search URL for that file:

https://github.com/Adjuvo/SenseGlove-Unity/search?q=SG_Grabable.cs


(If you later want to open local files in VS Code instead, set SCRIPT_DIR to your repo path and implement a local-first fallback.)

🧰 Troubleshooting
“Error: 'name'”

Your JSON had some entries that weren’t proper objects (e.g., plain strings).
Fix: The assistant already self-heals. Delete the file and run:

python github_updater.py
python sengeglove_cli.py

“[LLM Error] …” or no reply

Verify Ollama path and model:

"C:\Users\<you>\AppData\Local\Programs\Ollama\ollama.exe" list
"C:\Users\<you>\AppData\Local\Programs\Ollama\ollama.exe" run llama3.2


Update OLLAMA_PATH and OLLAMA_MODEL in sengeglove_cli.py.

“rate limit exceeded”

Set the token and reopen your terminal:

setx GITHUB_TOKEN "ghp_yourTokenHere"


Run the updater again:

python github_updater.py

“Could not open GitHub link”

Windows requires shell=True for start:
(Already handled in the code.)

Make sure your default browser is set.

🔍 FAQ

Q: Do I need a local clone of SenseGlove-Unity?
A: No. In GitHub-only mode, everything opens online.

Q: Can it open in VS Code locally?
A: Yes. Set SCRIPT_DIR to your local repo path and change open_script() to search the disk first.

Q: Does it work offline?
A: The assistant uses your local JSON + local LLM (Ollama), so yes.
GitHub updates require internet.

Q: What permissions does the GitHub token need?
A: For a public repo, no scopes needed. It’s only to improve rate limits.

🧩 Architecture (high level)
+-----------------------+           +---------------------------+
|  sengeglove_cli.py    |  <----->  |  senseglove_index_with_   |
|  (GUI + LLM prompts)  |           |  functions.json           |
+-----^-----------^-----+           +---------------------------+
      |           |                            ^
      |           |                            |
      |       Ollama (local LLM)               |
      |           |                            |
      v           |                            |
+-------------------------+         +----------+-----------+
| github_updater.py       |  ---->  | GitHub API (contents,|
| (fetch/parse/build JSON)|         | trees)               |
+-------------------------+         +----------------------+

✅ Quick Start (TL;DR)
# 1) (Once) Install Ollama and pull the model
ollama pull llama3.2

# 2) (Recommended) Set GitHub token
setx GITHUB_TOKEN "ghp_yourTokenHere"  # restart terminal afterward

# 3) Build/update the index
python github_updater.py

# 4) Launch the assistant
python sengeglove_cli.py
