# Unity Windsurf MCP for Windsurf

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

Unity Windsurf MCP connects your tools using two components:

1. **Unity Windsurf MCP Bridge**: A Unity package running inside the Editor (Installed via Package Manager)
2. **Unity Windsurf MCP Server**: A Python server that runs locally, communicating between the Unity Bridge and your MCP Client

Flow: `[Your LLM via MCP Client] <-> [Unity Windsurf MCP Server (Python)] <-> [Unity Windsurf MCP Bridge (Unity Editor)]`

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
4. Enter: `https://github.com/isekream/WindsurfUnityMCP.git#main?path=/UnityMcpBridge`
5. Click Add

### Step 2: Set Up the MCP Server

1. Clone this repository: `git clone https://github.com/windsurf/unity-mcp.git`
2. Navigate to the cloned directory
3. Run the server:
   - On macOS/Linux: `./start_server.sh`
   - On Windows: Double-click `start_server.bat`
4. The server will start on port 8000 by default

### Step 3: Configure Your MCP Client

Connect your Windsurf MCP Client to the Python server you installed in Step 2.

#### Option A: Auto-Configure (Recommended for Windsurf)

1. In Unity, go to Window > Unity Windsurf MCP
2. Click Auto Configure Windsurf
3. Look for a green status indicator 🟢 and "Connected"

#### Option B: Manual Configuration

If Auto-Configure fails or you use a different client:

1. Find your MCP Client's configuration file (Check client documentation)
   - Windsurf Example (macOS): `~/Library/Application Support/Windsurf/windsurf_config.json`
   - Windsurf Example (Windows): `%APPDATA%\Windsurf\windsurf_config.json`
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

1. Open your Unity Project. The Unity Windsurf MCP Bridge (package) should connect automatically. Check status via Window > Unity Windsurf MCP
2. Start your Windsurf MCP Client. It should automatically launch the Unity Windsurf MCP Server (Python) using the configuration from Installation
3. Interact! Unity tools should now be available in your MCP Client

Example Prompt: "Create a 3D player controller."

## Troubleshooting ❓

- Check the Unity Console for any error messages
- Verify that the Python server is running
- Ensure your MCP client configuration is correct
- Restart Unity and your MCP client if needed

## License 📜

MIT License
