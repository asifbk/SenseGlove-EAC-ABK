@echo off
title 🧠 Building SenseGlove Assistant
echo ===============================
echo   Building SenseGlove Assistant
echo ===============================
echo.

REM --- Automatically detect where the BAT file is located ---
setlocal
set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%"

REM --- Check if senseglove_cli.py exists ---
if not exist "%SCRIPT_DIR%senseglove_cli.py" (
    echo ❌ ERROR: senseglove_cli.py not found in "%SCRIPT_DIR%"
    echo Please place this BAT file in the same folder as senseglove_cli.py
    pause
    exit /b
)

REM --- Confirm Python path ---
echo 🔍 Using Python from: %PYTHONPATH%
where python
if errorlevel 1 (
    echo ❌ Python not found. Please install Python and add it to PATH.
    pause
    exit /b
)

REM --- Remove previous build folders ---
if exist build rmdir /s /q build
if exist dist rmdir /s /q dist
if exist __pycache__ rmdir /s /q __pycache__

REM --- Run PyInstaller ---
echo 🚀 Building executable with PyInstaller...
python -m PyInstaller --noconfirm --onefile --windowed "%SCRIPT_DIR%senseglove_cli.py"

REM --- Check result ---
if exist "%SCRIPT_DIR%dist\senseglove_cli.exe" (
    echo ✅ Build complete!
    echo Your executable is here: "%SCRIPT_DIR%dist\senseglove_cli.exe"
    echo.
    echo 📂 Opening dist folder...
    start "" "%SCRIPT_DIR%dist"
) else (
    echo ⚠️ Something went wrong. Check for errors above.
)

echo.
pause