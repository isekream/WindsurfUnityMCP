#!/bin/bash
# Windsurf Unity MCP Server Startup Script

echo "Starting Windsurf Unity MCP Server..."

# Navigate to the server directory
cd "$(dirname "$0")/UnityMcpServer"

# Check if uv is installed
if ! command -v uv &> /dev/null; then
    echo "uv package manager not found. Installing..."
    pip install uv
fi

# Install dependencies if needed
echo "Checking dependencies..."
uv pip install -r requirements.txt

# Start the server
echo "Starting server..."
cd src
python server.py

# This line will only be reached if the server exits
echo "Server stopped."
