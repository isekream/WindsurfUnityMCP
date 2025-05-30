# Unity MCP for Windsurf

A Model Context Protocol (MCP) server that allows Windsurf to perform Unity Editor actions.

## Key Features 🚀
- 🗣️ Natural Language Control: Instruct your LLM to perform Unity tasks
- 🛠️ Powerful Tools: Manage assets, scenes, materials, scripts, and editor functions
- 🤖 Automation: Automate repetitive Unity workflows
- 🧩 Extensible: Designed to work with various MCP Clients

Your LLM can use functions like:
- `read_console`: Gets messages from or clears the console
- `manage_script`: Manages C# scripts (create, read, update, delete)
- `manage_editor`: Controls and queries the editor's state and settings
- `manage_scene`: Manages scenes (load, save, create, get hierarchy, etc.)
- `manage_asset`: Performs asset operations (import, create, modify, delete, etc.)
- `manage_gameobject`: Manages GameObjects: create, modify, delete, find, and component operations
- `execute_menu_item`: Executes a menu item via its path (e.g., "File/Save Project")

## How It Works 🤔
Unity MCP connects your tools using two components:
1. **Unity MCP Bridge**: A Unity package running inside the Editor (Installed via Package Manager)
2. **Unity MCP Server**: A Python server that runs locally, communicating between the Unity Bridge and your MCP Client

Flow: `[Your LLM via MCP Client] <-> [Unity MCP Server (Python)] <-> [Unity MCP Bridge (Unity Editor)]`

## Prerequisites
- Git CLI: For cloning the server code
- Python: Version 3.10 or newer
- Unity Hub & Editor: Version 2020.3 LTS or newer
- uv (Python package manager): `pip install uv`

## Installation ⚙️

### Step 1: Install the Unity Package (Bridge)
1. Open your Unity project
2. Go to Window > Package Manager
3. Click + -> Add package from git URL...
4. Enter: `https://github.com/yourusername/unity-mcp.git?path=/UnityMcpBridge`
5. Click Add
6. The MCP Server should automatically be installed onto your machine as a result of this process

### Step 2: Configure Your MCP Client
Connect your MCP Client (Claude, Cursor, etc.) to the Python server you installed in Step 1.

#### Option A: Auto-Configure (Recommended for Claude/Cursor)
1. In Unity, go to Window > Unity MCP
2. Click Auto Configure Claude or Auto Configure Cursor
3. Look for a green status indicator 🟢 and "Connected"

#### Option B: Manual Configuration
If Auto-Configure fails or you use a different client:
1. Find your MCP Client's configuration file (Check client documentation)
   - Claude Example (macOS): `~/Library/Application Support/Claude/claude_desktop_config.json`
   - Claude Example (Windows): `%APPDATA%\Claude\claude_desktop_config.json`
2. Edit the file to add/update the mcpServers section, using the exact paths from Step 1.

macOS:
```json
{
  "mcpServers": {
    "UnityMCP": {
      "command": "uv",
      "args": [
        "run",
        "--directory",
        "/usr/local/bin/UnityMCP/UnityMcpServer/src",
        "server.py"
      ]
    }
    // ... other servers might be here ...
  }
}
```

Windows:
```json
{
  "mcpServers": {
    "UnityMCP": {
      "command": "uv",
      "args": [
        "run",
        "--directory",
        "C:\\Users\\YOUR_USERNAME\\AppData\\Local\\Programs\\UnityMCP\\UnityMcpServer\\src",
        "server.py"
      ]
    }
    // ... other servers might be here ...
  }
}
```

## Usage ▶️
1. Open your Unity Project. The Unity MCP Bridge (package) should connect automatically. Check status via Window > Unity MCP
2. Start your MCP Client (Claude, Cursor, etc.). It should automatically launch the Unity MCP Server (Python) using the configuration from Installation
3. Interact! Unity tools should now be available in your MCP Client

Example Prompt: "Create a 3D player controller."

## Troubleshooting ❓
- Check the Unity Console for any error messages
- Verify that the Python server is running
- Ensure your MCP client configuration is correct
- Restart Unity and your MCP client if needed

## License 📜
MIT License
