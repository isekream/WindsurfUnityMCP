#!/usr/bin/env python3
"""
Unity Windsurf MCP Server
This server acts as a bridge between Windsurf MCP clients and the Unity Editor.
It implements the Model Context Protocol (MCP) to expose Unity functionality to LLMs.
"""

import os
import sys
import json
import logging
import asyncio
import traceback
from typing import Dict, Any, List, Optional, Union, Callable
from enum import Enum

import uvicorn
from fastapi import FastAPI, HTTPException, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[logging.StreamHandler()]
)
logger = logging.getLogger(__name__)

# Create FastAPI app
app = FastAPI(title="Windsurf Unity MCP Server")

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# WebSocket connection manager
class ConnectionManager:
    def __init__(self):
        self.active_connections: List[WebSocket] = []
        self.unity_connection: Optional[WebSocket] = None
        self.pending_requests: Dict[str, asyncio.Future] = {}
        
    async def connect(self, websocket: WebSocket, client_type: str):
        await websocket.accept()
        self.active_connections.append(websocket)
        if client_type == "unity":
            self.unity_connection = websocket
            logger.info("Unity Editor connected")
        else:
            logger.info(f"Client connected: {client_type}")
    
    def disconnect(self, websocket: WebSocket):
        self.active_connections.remove(websocket)
        if websocket == self.unity_connection:
            self.unity_connection = None
            logger.info("Unity Editor disconnected")
        else:
            logger.info("Client disconnected")
    
    async def send_to_unity(self, message: Dict[str, Any]) -> Dict[str, Any]:
        if not self.unity_connection:
            raise Exception("No Unity connection available")
        
        request_id = message.get("id", str(id(message)))
        message["id"] = request_id
        
        # Create a future to wait for the response
        future = asyncio.Future()
        self.pending_requests[request_id] = future
        
        # Send the message to Unity
        await self.unity_connection.send_json(message)
        
        try:
            # Wait for the response with a timeout
            response = await asyncio.wait_for(future, timeout=30.0)
            return response
        except asyncio.TimeoutError:
            del self.pending_requests[request_id]
            raise Exception("Request to Unity timed out")
    
    def handle_unity_response(self, response: Dict[str, Any]):
        request_id = response.get("id")
        if request_id in self.pending_requests:
            future = self.pending_requests.pop(request_id)
            if not future.done():
                future.set_result(response)
        else:
            logger.warning(f"Received response for unknown request ID: {request_id}")

# Initialize connection manager
manager = ConnectionManager()

# MCP Models
class ResourceListRequest(BaseModel):
    cursor: Optional[str] = None

class ResourceListResponse(BaseModel):
    resources: List[Dict[str, Any]]
    cursor: Optional[str] = None

class ResourceReadRequest(BaseModel):
    uri: str

class ResourceReadResponse(BaseModel):
    content: Any

# MCP Function Models
class ExecuteMenuItemRequest(BaseModel):
    menu_path: str
    action: str = "execute"
    parameters: Optional[Dict[str, Any]] = None

class ManageScriptRequest(BaseModel):
    action: str  # 'create', 'read', 'update', 'delete'
    name: str
    path: Optional[str] = "Assets/"
    contents: Optional[str] = None
    script_type: Optional[str] = None
    namespace: Optional[str] = None

class ManageEditorRequest(BaseModel):
    action: str  # 'play', 'pause', 'get_state', 'set_active_tool', etc.
    wait_for_completion: Optional[bool] = None
    tool_name: Optional[str] = None
    tag_name: Optional[str] = None
    layer_name: Optional[str] = None

class ManageSceneRequest(BaseModel):
    action: str  # 'load', 'save', 'create', 'get_hierarchy', etc.
    name: Optional[str] = None
    path: Optional[str] = "Assets/"
    build_index: Optional[int] = None

class ManageAssetRequest(BaseModel):
    action: str  # 'import', 'create', 'modify', 'delete', 'duplicate', etc.
    path: str
    asset_type: Optional[str] = None
    properties: Optional[Dict[str, Any]] = None
    destination: Optional[str] = None
    search_pattern: Optional[str] = None
    filter_type: Optional[str] = None
    filter_date_after: Optional[str] = None
    page_number: Optional[int] = None
    page_size: Optional[int] = None
    generate_preview: Optional[bool] = False

class ManageGameObjectRequest(BaseModel):
    action: str  # 'create', 'modify', 'find', 'add_component', etc.
    target: Optional[str] = None
    search_method: Optional[str] = None
    name: Optional[str] = None
    tag: Optional[str] = None
    parent: Optional[str] = None
    layer: Optional[str] = None
    position: Optional[List[float]] = None
    rotation: Optional[List[float]] = None
    scale: Optional[List[float]] = None
    component_name: Optional[str] = None
    component_properties: Optional[Dict[str, Dict[str, Any]]] = None
    components_to_add: Optional[List[str]] = None
    components_to_remove: Optional[List[str]] = None
    search_term: Optional[str] = None
    search_in_children: Optional[bool] = False
    search_inactive: Optional[bool] = False
    find_all: Optional[bool] = False
    set_active: Optional[bool] = None
    primitive_type: Optional[str] = None
    prefab_path: Optional[str] = None
    save_as_prefab: Optional[bool] = False
    prefab_folder: Optional[str] = "Assets/Prefabs"

class ReadConsoleRequest(BaseModel):
    action: str  # 'get' or 'clear'
    types: Optional[List[str]] = None  # 'error', 'warning', 'log', 'all'
    count: Optional[int] = None
    filter_text: Optional[str] = None
    since_timestamp: Optional[str] = None
    format: Optional[str] = None  # 'plain', 'detailed', 'json'
    include_stacktrace: Optional[bool] = None

# MCP Function Response
class FunctionResponse(BaseModel):
    success: bool
    message: Optional[str] = None
    data: Optional[Any] = None
    error: Optional[str] = None

# MCP Routes
@app.get("/")
async def root():
    print("\033[92m✓ Windsurf Unity MCP Server running at http://localhost:" + str(port) + "\033[0m")
    return {"message": "Windsurf Unity MCP Server is running"}

@app.post("/list")
async def list_resources(request: ResourceListRequest) -> ResourceListResponse:
    """List available MCP resources (functions)"""
    resources = [
        {
            "uri": "execute_menu_item",
            "description": "Executes a Unity Editor menu item via its path (e.g., \"File/Save Project\").\n\n        Args:\n            ctx: The MCP context.\n            menu_path: The full path of the menu item to execute.\n            action: The operation to perform (default: 'execute').\n            parameters: Optional parameters for the menu item (rarely used).\n\n        Returns:\n            A dictionary indicating success or failure, with optional message/error.\n        ",
            "schema": {
                "type": "object",
                "properties": {
                    "menu_path": {"title": "Menu Path", "type": "string"},
                    "action": {"title": "Action", "type": "string", "default": "execute"},
                    "parameters": {"title": "Parameters", "type": "object"}
                }
            }
        },
        {
            "uri": "manage_script",
            "description": "Manages C# scripts in Unity (create, read, update, delete).\n        Make reference variables public for easier access in the Unity Editor.\n\n        Args:\n            action: Operation ('create', 'read', 'update', 'delete').\n            name: Script name (no .cs extension).\n            path: Asset path (default: \"Assets/\").\n            contents: C# code for 'create'/'update'.\n            script_type: Type hint (e.g., 'MonoBehaviour').\n            namespace: Script namespace.\n\n        Returns:\n            Dictionary with results ('success', 'message', 'data').\n        ",
            "schema": {
                "type": "object",
                "properties": {
                    "action": {"title": "Action", "type": "string"},
                    "name": {"title": "Name", "type": "string"},
                    "path": {"title": "Path", "type": "string"},
                    "contents": {"title": "Contents", "type": "string"},
                    "script_type": {"title": "Script Type", "type": "string"},
                    "namespace": {"title": "Namespace", "type": "string"}
                }
            }
        },
        {
            "uri": "manage_editor",
            "description": "Controls and queries the Unity editor's state and settings.\n\n        Args:\n            action: Operation (e.g., 'play', 'pause', 'get_state', 'set_active_tool', 'add_tag').\n            wait_for_completion: Optional. If True, waits for certain actions.\n            Action-specific arguments (e.g., tool_name, tag_name, layer_name).\n\n        Returns:\n            Dictionary with operation results ('success', 'message', 'data').\n        ",
            "schema": {
                "type": "object",
                "properties": {
                    "action": {"title": "Action", "type": "string"},
                    "wait_for_completion": {"title": "Wait For Completion", "type": "boolean"},
                    "tool_name": {"title": "Tool Name", "type": "string"},
                    "tag_name": {"title": "Tag Name", "type": "string"},
                    "layer_name": {"title": "Layer Name", "type": "string"}
                }
            }
        },
        {
            "uri": "manage_scene",
            "description": "Manages Unity scenes (load, save, create, get hierarchy, etc.).\n\n        Args:\n            action: Operation (e.g., 'load', 'save', 'create', 'get_hierarchy').\n            name: Scene name (no extension) for create/load/save.\n            path: Asset path for scene operations (default: \"Assets/\").\n            build_index: Build index for load/build settings actions.\n            # Add other action-specific args as needed (e.g., for hierarchy depth)\n\n        Returns:\n            Dictionary with results ('success', 'message', 'data').\n        ",
            "schema": {
                "type": "object",
                "properties": {
                    "action": {"title": "Action", "type": "string"},
                    "name": {"title": "Name", "type": "string"},
                    "path": {"title": "Path", "type": "string"},
                    "build_index": {"title": "Build Index", "type": "integer"}
                }
            }
        },
        {
            "uri": "manage_asset",
            "description": "Performs asset operations (import, create, modify, delete, etc.) in Unity.\n\n        Args:\n            ctx: The MCP context.\n            action: Operation to perform (e.g., 'import', 'create', 'modify', 'delete', 'duplicate', 'move', 'rename', 'search', 'get_info', 'create_folder', 'get_components').\n            path: Asset path (e.g., \"Materials/MyMaterial.mat\") or search scope.\n            asset_type: Asset type (e.g., 'Material', 'Folder') - required for 'create'.\n            properties: Dictionary of properties for 'create'/'modify'.\n            destination: Target path for 'duplicate'/'move'.\n            search_pattern: Search pattern (e.g., '*.prefab').\n            filter_*: Filters for search (type, date).\n            page_*: Pagination for search.\n\n        Returns:\n            A dictionary with operation results ('success', 'data', 'error').\n        ",
            "schema": {
                "type": "object",
                "properties": {
                    "action": {"title": "Action", "type": "string"},
                    "path": {"title": "Path", "type": "string"},
                    "asset_type": {"title": "Asset Type", "type": "string"},
                    "properties": {"title": "Properties", "type": "object"},
                    "destination": {"title": "Destination", "type": "string"},
                    "search_pattern": {"title": "Search Pattern", "type": "string"},
                    "filter_type": {"title": "Filter Type", "type": "string"},
                    "filter_date_after": {"title": "Filter Date After", "type": "string"},
                    "page_number": {"title": "Page Number", "type": "integer"},
                    "page_size": {"title": "Page Size", "type": "integer"},
                    "generate_preview": {"title": "Generate Preview", "type": "boolean", "default": False}
                }
            }
        },
        {
            "uri": "manage_gameobject",
            "description": "Manages GameObjects: create, modify, delete, find, and component operations.\n\n        Args:\n            action: Operation (e.g., 'create', 'modify', 'find', 'add_component', 'remove_component', 'set_component_property').\n            target: GameObject identifier (name or path string) for modify/delete/component actions.\n            search_method: How to find objects ('by_name', 'by_id', 'by_path', etc.). Used with 'find' and some 'target' lookups.\n            name: GameObject name - used for both 'create' (initial name) and 'modify' (rename).\n            tag: Tag name - used for both 'create' (initial tag) and 'modify' (change tag).\n            parent: Parent GameObject reference - used for both 'create' (initial parent) and 'modify' (change parent).\n            layer: Layer name - used for both 'create' (initial layer) and 'modify' (change layer).\n            component_properties: Dict mapping Component names to their properties to set.\n                                  Example: {\"Rigidbody\": {\"mass\": 10.0, \"useGravity\": True}},\n                                  To set references:\n                                  - Use asset path string for Prefabs/Materials, e.g., {\"MeshRenderer\": {\"material\": \"Assets/Materials/MyMat.mat\"}}\n                                  - Use a dict for scene objects/components, e.g.:\n                                    {\"MyScript\": {\"otherObject\": {\"find\": \"Player\", \"method\": \"by_name\"}}} (assigns GameObject)\n                                    {\"MyScript\": {\"playerHealth\": {\"find\": \"Player\", \"component\": \"HealthComponent\"}}} (assigns Component)\n                                  Example set nested property:\n                                  - Access shared material: {\"MeshRenderer\": {\"sharedMaterial.color\": [1, 0, 0, 1]}}\n            components_to_add: List of component names to add.\n            Action-specific arguments (e.g., position, rotation, scale for create/modify;\n                     component_name for component actions;\n                     search_term, find_all for 'find').\n\n        Returns:\n            Dictionary with operation results ('success', 'message', 'data').\n        ",
            "schema": {
                "type": "object",
                "properties": {
                    "action": {"title": "Action", "type": "string"},
                    "target": {"title": "Target", "type": "string"},
                    "search_method": {"title": "Search Method", "type": "string"},
                    "name": {"title": "Name", "type": "string"},
                    "tag": {"title": "Tag", "type": "string"},
                    "parent": {"title": "Parent", "type": "string"},
                    "layer": {"title": "Layer", "type": "string"},
                    "position": {"title": "Position", "type": "array", "items": {"type": "number"}},
                    "rotation": {"title": "Rotation", "type": "array", "items": {"type": "number"}},
                    "scale": {"title": "Scale", "type": "array", "items": {"type": "number"}},
                    "component_name": {"title": "Component Name", "type": "string"},
                    "component_properties": {"title": "Component Properties", "type": "object", "additionalProperties": {"type": "object"}},
                    "components_to_add": {"title": "Components To Add", "type": "array", "items": {"type": "string"}},
                    "components_to_remove": {"title": "Components To Remove", "type": "array", "items": {"type": "string"}},
                    "search_term": {"title": "Search Term", "type": "string"},
                    "search_in_children": {"title": "Search In Children", "type": "boolean", "default": False},
                    "search_inactive": {"title": "Search Inactive", "type": "boolean", "default": False},
                    "find_all": {"title": "Find All", "type": "boolean", "default": False},
                    "set_active": {"title": "Set Active", "type": "boolean"},
                    "primitive_type": {"title": "Primitive Type", "type": "string"},
                    "prefab_path": {"title": "Prefab Path", "type": "string"},
                    "save_as_prefab": {"title": "Save As Prefab", "type": "boolean", "default": False},
                    "prefab_folder": {"title": "Prefab Folder", "type": "string", "default": "Assets/Prefabs"}
                }
            }
        },
        {
            "uri": "read_console",
            "description": "Gets messages from or clears the Unity Editor console.\n\n        Args:\n            ctx: The MCP context.\n            action: Operation ('get' or 'clear').\n            types: Message types to get ('error', 'warning', 'log', 'all').\n            count: Max messages to return.\n            filter_text: Text filter for messages.\n            since_timestamp: Get messages after this timestamp (ISO 8601).\n            format: Output format ('plain', 'detailed', 'json').\n            include_stacktrace: Include stack traces in output.\n\n        Returns:\n            Dictionary with results. For 'get', includes 'data' (messages).\n        ",
            "schema": {
                "type": "object",
                "properties": {
                    "action": {"title": "Action", "type": "string"},
                    "types": {"title": "Types", "type": "array", "items": {"type": "string"}},
                    "count": {"title": "Count", "type": "integer"},
                    "filter_text": {"title": "Filter Text", "type": "string"},
                    "since_timestamp": {"title": "Since Timestamp", "type": "string"},
                    "format": {"title": "Format", "type": "string"},
                    "include_stacktrace": {"title": "Include Stacktrace", "type": "boolean"}
                }
            }
        }
    ]
    
    return ResourceListResponse(resources=resources, cursor=None)

@app.post("/read")
async def read_resource(request: ResourceReadRequest) -> ResourceReadResponse:
    """Read a resource's schema"""
    # This is a simplified version that just returns the schema
    # In a real implementation, you would have more logic here
    return ResourceReadResponse(content={"message": f"Schema for {request.uri}"})

@app.post("/execute_menu_item")
async def execute_menu_item(request: ExecuteMenuItemRequest) -> FunctionResponse:
    """Execute a Unity Editor menu item"""
    try:
        response = await manager.send_to_unity({
            "function": "execute_menu_item",
            "params": request.dict()
        })
        return FunctionResponse(**response)
    except Exception as e:
        logger.error(f"Error executing menu item: {str(e)}")
        return FunctionResponse(success=False, error=str(e))

@app.post("/manage_script")
async def manage_script(request: ManageScriptRequest) -> FunctionResponse:
    """Manage C# scripts in Unity"""
    try:
        response = await manager.send_to_unity({
            "function": "manage_script",
            "params": request.dict()
        })
        return FunctionResponse(**response)
    except Exception as e:
        logger.error(f"Error managing script: {str(e)}")
        return FunctionResponse(success=False, error=str(e))

@app.post("/manage_editor")
async def manage_editor(request: ManageEditorRequest) -> FunctionResponse:
    """Control and query the Unity editor's state and settings"""
    try:
        response = await manager.send_to_unity({
            "function": "manage_editor",
            "params": request.dict()
        })
        return FunctionResponse(**response)
    except Exception as e:
        logger.error(f"Error managing editor: {str(e)}")
        return FunctionResponse(success=False, error=str(e))

@app.post("/manage_scene")
async def manage_scene(request: ManageSceneRequest) -> FunctionResponse:
    """Manage Unity scenes"""
    try:
        response = await manager.send_to_unity({
            "function": "manage_scene",
            "params": request.dict()
        })
        return FunctionResponse(**response)
    except Exception as e:
        logger.error(f"Error managing scene: {str(e)}")
        return FunctionResponse(success=False, error=str(e))

@app.post("/manage_asset")
async def manage_asset(request: ManageAssetRequest) -> FunctionResponse:
    """Perform asset operations in Unity"""
    try:
        response = await manager.send_to_unity({
            "function": "manage_asset",
            "params": request.dict()
        })
        return FunctionResponse(**response)
    except Exception as e:
        logger.error(f"Error managing asset: {str(e)}")
        return FunctionResponse(success=False, error=str(e))

@app.post("/manage_gameobject")
async def manage_gameobject(request: ManageGameObjectRequest) -> FunctionResponse:
    """Manage GameObjects in Unity"""
    try:
        response = await manager.send_to_unity({
            "function": "manage_gameobject",
            "params": request.dict()
        })
        return FunctionResponse(**response)
    except Exception as e:
        logger.error(f"Error managing GameObject: {str(e)}")
        return FunctionResponse(success=False, error=str(e))

@app.post("/read_console")
async def read_console(request: ReadConsoleRequest) -> FunctionResponse:
    """Get messages from or clear the Unity Editor console"""
    try:
        response = await manager.send_to_unity({
            "function": "read_console",
            "params": request.dict()
        })
        return FunctionResponse(**response)
    except Exception as e:
        logger.error(f"Error reading console: {str(e)}")
        return FunctionResponse(success=False, error=str(e))

# WebSocket endpoint for Unity and MCP clients
@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket):
    client_type = "unknown"
    try:
        # Accept the connection
        await websocket.accept()
        
        # Get the client type from the first message
        first_message = await websocket.receive_json()
        client_type = first_message.get("client_type", "unknown")
        
        # Connect to the manager
        await manager.connect(websocket, client_type)
        
        # Handle messages
        while True:
            message = await websocket.receive_json()
            
            if client_type == "unity":
                # Handle responses from Unity
                manager.handle_unity_response(message)
            else:
                # Handle requests from MCP clients
                try:
                    function_name = message.get("function")
                    params = message.get("params", {})
                    
                    if not function_name:
                        await websocket.send_json({
                            "id": message.get("id"),
                            "success": False,
                            "error": "Missing function name"
                        })
                        continue
                    
                    # Forward the request to Unity
                    response = await manager.send_to_unity(message)
                    await websocket.send_json(response)
                    
                except Exception as e:
                    logger.error(f"Error handling client request: {str(e)}")
                    await websocket.send_json({
                        "id": message.get("id"),
                        "success": False,
                        "error": str(e)
                    })
    
    except WebSocketDisconnect:
        manager.disconnect(websocket)
    except Exception as e:
        logger.error(f"WebSocket error: {str(e)}")
        logger.error(traceback.format_exc())
        try:
            manager.disconnect(websocket)
        except:
            pass

if __name__ == "__main__":
    port = int(os.environ.get("PORT", 8000))
    uvicorn.run("server:app", host="0.0.0.0", port=port, reload=True)
