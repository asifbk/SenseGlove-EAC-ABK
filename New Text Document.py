#!/usr/bin/env python3
# ---------------------------------------------------------
# SenseGlove JSON Function Mapper
# ---------------------------------------------------------
# Adds every function/method from each .cs script into the
# existing JSON index.
# ---------------------------------------------------------

import os, re, json

# --- CONFIG ---
SCRIPT_DIR = r"C:\Users\mkarim1\Desktop\My Unity Projects\SenseGlove\SenseGlove-Unity-master\SenseGlove-Unity-master\SenseGlove\Scripts"
INPUT_JSON = "senseglove_index.json"
OUTPUT_JSON = "senseglove_index_with_functions.json"

# --- FUNCTION PATTERN ---
# Matches function signatures like:
# public void ApplyVibration(float amp)
# private static int ComputeForce() { ... }
function_pattern = re.compile(
    r"(public|private|protected|internal)?\s*(static\s*)?[\w<>\[\]]+\s+(\w+)\s*\([^)]*\)",
    re.MULTILINE
)

def extract_functions_from_cs(file_path):
    """Extract function signatures from a .cs file"""
    functions = []
    try:
        with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
            code = f.read()
            for match in function_pattern.finditer(code):
                full_signature = match.group(0).strip()
                func_name = match.group(3)
                functions.append({
                    "name": func_name,
                    "signature": full_signature
                })
    except Exception as e:
        print(f"⚠️ Error reading {file_path}: {e}")
    return functions


def build_function_index(base_dir):
    """Scan all .cs files for function definitions"""
    index = {}
    for root, _, files in os.walk(base_dir):
        for file in files:
            if file.endswith(".cs"):
                full_path = os.path.join(root, file)
                funcs = extract_functions_from_cs(full_path)
                index[file] = funcs
    return index


def update_json_with_functions(input_json, function_index, output_json):
    """Merge the new function data into the original JSON"""
    with open(input_json, "r", encoding="utf-8") as f:
        data = json.load(f)

    for entry in data:
        name = entry.get("name")
        if name in function_index:
            entry["functions"] = function_index[name]
        else:
            entry["functions"] = []

    with open(output_json, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print(f"✅ Updated JSON written to: {output_json}")


if __name__ == "__main__":
    print("🔍 Scanning for functions in .cs scripts...")
    func_index = build_function_index(SCRIPT_DIR)
    print(f"📄 Found {len(func_index)} scripts.")
    update_json_with_functions(INPUT_JSON, func_index, OUTPUT_JSON)
