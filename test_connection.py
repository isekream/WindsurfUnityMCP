#!/usr/bin/env python3
"""
Windsurf Unity MCP Connection Test Script
This script tests the connection to the Windsurf Unity MCP server and verifies that it's working correctly.
"""

import asyncio
import json
import sys
import websockets

SERVER_URL = "ws://localhost:8000/ws"

async def test_connection():
    """Test the connection to the Windsurf Unity MCP server."""
    print(f"Connecting to Windsurf Unity MCP server at {SERVER_URL}...")
    
    try:
        async with websockets.connect(SERVER_URL) as websocket:
            # Send initial message to identify as a test client
            await websocket.send(json.dumps({
                "client_type": "mcp_client",
                "client_name": "Windsurf_Test"
            }))
            
            print("Connected successfully! ✅")
            print("Sending test request to get editor state...")
            
            # Send a request to get the editor state
            await websocket.send(json.dumps({
                "id": "test_request",
                "function": "manage_editor",
                "params": {
                    "action": "get_state"
                }
            }))
            
            # Wait for the response
            response = await websocket.recv()
            response_data = json.loads(response)
            
            print("\nResponse from Unity:")
            print(json.dumps(response_data, indent=2))
            
            if response_data.get("success"):
                print("\nTest completed successfully! ✅")
                print("The Windsurf Unity MCP system is working correctly.")
            else:
                print("\nTest failed! ❌")
                print(f"Error: {response_data.get('error', 'Unknown error')}")
                print("Make sure Unity is running with the Windsurf Unity MCP package installed.")
                
    except ConnectionRefusedError:
        print("Connection refused! ❌")
        print("Make sure the Unity Windsurf MCP server is running on port 8000.")
    except Exception as e:
        print(f"Error: {str(e)} ❌")
        print("Make sure the Unity Windsurf MCP server is running and accessible.")

if __name__ == "__main__":
    try:
        asyncio.run(test_connection())
    except KeyboardInterrupt:
        print("\nTest interrupted by user.")
        sys.exit(0)
