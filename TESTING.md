# Unity Windsurf MCP Testing Guide

This guide will help you verify that your Unity Windsurf MCP system is working correctly.

## Prerequisites

- Python 3.10 or newer
- Unity Editor with the Unity Windsurf MCP Bridge package installed
- Unity Windsurf MCP Server running

## Running the Test

1. Make sure the Unity Editor is open with a project that has the Unity Windsurf MCP Bridge package installed.

2. Start the Unity Windsurf MCP Server by running:
   - On macOS/Linux: `./start_server.sh`
   - On Windows: Double-click `start_server.bat`

3. In Unity, go to Window > Unity Windsurf MCP to open the Unity Windsurf MCP window.

4. Click "Connect" to connect the Unity Editor to the MCP server.

5. Run the test script:
   ```bash
   python test_connection.py
   ```

6. If everything is working correctly, you should see a success message and a response from Unity.

## Troubleshooting

If the test fails, check the following:

1. **Server Connection Issues**:
   - Make sure the Unity Windsurf MCP Server is running on port 8000
   - Check if there are any firewall issues blocking the connection

2. **Unity Connection Issues**:
   - Make sure Unity is running with the Unity Windsurf MCP Bridge package installed
   - Check if the Unity Windsurf MCP Bridge is connected to the server (the status should be "Connected" in the Unity Windsurf MCP window)

3. **Python Issues**:
   - Make sure you have the required dependencies installed: `pip install websockets`

## Manual Testing

You can also test the connection manually using the Unity Windsurf MCP window:

1. Open Unity and go to Window > Unity Windsurf MCP
2. Click "Connect" to connect to the server
3. Check if the status changes to "Connected" (green)
4. Try clicking "Auto Configure Windsurf" to configure the Windsurf MCP client

If the status shows "Connected" and there are no errors, the Unity Windsurf MCP system is working correctly.
