@echo off
REM Windsurf Unity MCP Server Startup Script for Windows

echo Starting Windsurf Unity MCP Server...

REM Navigate to the server directory
cd /d "%~dp0UnityMcpServer"

REM Check if uv is installed
where uv >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo uv package manager not found. Installing...
    pip install uv
)

REM Install dependencies if needed
echo Checking dependencies...
uv pip install -r requirements.txt

REM Start the server
echo Starting server...
cd src
python server.py

REM This line will only be reached if the server exits
echo Server stopped.
