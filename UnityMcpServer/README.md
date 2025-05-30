# Unity Windsurf MCP Server

This is the Python server component of the Unity Windsurf MCP system that enables communication between Windsurf MCP clients and the Unity Editor.

## Prerequisites

- Python 3.10 or newer
- uv package manager: `pip install uv`

## Installation

1. Install the required dependencies:

```bash
cd UnityMcpServer
uv pip install -r requirements.txt
```

2. Run the server:

```bash
cd src
python server.py
```

By default, the server will run on port 8000. You can change this by setting the `PORT` environment variable.

## Configuration

The server doesn't require any configuration files. It will automatically connect to any Unity Editor instances running the Unity Windsurf MCP Bridge package.

## API

The server implements the Model Context Protocol (MCP) to expose Unity functionality to LLMs. It provides the following endpoints:

- `/list`: List available MCP resources (functions)
- `/read`: Read a resource's schema
- `/execute_menu_item`: Execute a Unity Editor menu item
- `/manage_script`: Manage C# scripts in Unity
- `/manage_editor`: Control and query the Unity editor's state and settings
- `/manage_scene`: Manage Unity scenes
- `/manage_asset`: Perform asset operations in Unity
- `/manage_gameobject`: Manage GameObjects in Unity
- `/read_console`: Get messages from or clear the Unity Editor console
- `/ws`: WebSocket endpoint for Unity and MCP clients

## WebSocket Communication

The server uses WebSockets for real-time communication between MCP clients and the Unity Editor. When a client connects to the WebSocket endpoint, it should send an initial message with a `client_type` field to identify itself.

Example:

```json
{
  "client_type": "unity",
  "unity_version": "2022.3.0f1"
}
```

or

```json
{
  "client_type": "mcp_client",
  "client_name": "Windsurf"
}
```

## Troubleshooting

If you encounter any issues:

1. Make sure the Unity Editor is running with the Unity Windsurf MCP Bridge package installed
2. Check that the server is running on the correct port
3. Verify that your MCP client is properly configured to use the server
4. Check the server logs for any error messages

## License

MIT License
