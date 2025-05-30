# Unity Windsurf MCP Bridge

A Unity package that enables communication between Unity and Windsurf MCP clients through the Model Context Protocol (MCP).

## Features

- Seamless integration with Unity Editor
- Real-time communication with Windsurf MCP clients
- Support for various Unity operations through MCP functions
- Easy configuration through a dedicated editor window

## Installation

### Option 1: Install via Git URL (Recommended)

1. Open your Unity project
2. Go to Window > Package Manager
3. Click the "+" button in the top-left corner
4. Select "Add package from git URL..."
5. Enter: `https://github.com/isekream/WindsurfUnityMCP.git?path=/UnityMcpBridge`
6. Click "Add"

### Option 2: Manual Installation

1. Download or clone this repository
2. Copy the `UnityMcpBridge` folder into your Unity project's `Packages` directory

## Getting Started

1. After installation, open the Unity Windsurf MCP window by going to Window > Unity Windsurf MCP
2. Make sure the Unity Windsurf MCP Server is running (see main repository for instructions)
3. Click "Connect" to establish a connection to the server
4. Use "Auto Configure Windsurf" to set up your Windsurf MCP client

## Usage

Once connected, the Unity Windsurf MCP Bridge enables your Windsurf MCP client to:

- Execute menu items
- Manage scripts (create, read, update, delete)
- Control the editor (play, pause, etc.)
- Manage scenes (load, save, create, etc.)
- Perform asset operations
- Manage GameObjects
- Read console messages

## Configuration

You can configure the Unity Windsurf MCP Bridge through the editor window:

- **Server URL**: The WebSocket URL of the Unity Windsurf MCP Server (default: `ws://localhost:8000/ws`)
- **Auto Connect**: Whether to automatically connect to the server when Unity starts
- **Debug Mode**: Enable for additional logging

## Troubleshooting

If you encounter issues:

1. Make sure the Unity Windsurf MCP Server is running
2. Check the Unity Console for error messages
3. Verify that the server URL is correct
4. Try restarting Unity and the server

## License

MIT License
