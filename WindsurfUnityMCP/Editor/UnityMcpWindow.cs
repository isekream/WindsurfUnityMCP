using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Windsurf.UnityMcp.Editor
{
    /// <summary>
    /// Editor window for Unity Windsurf MCP configuration and management
    /// </summary>
    public class UnityMcpWindow : EditorWindow
    {
        private string _serverUrl = "ws://localhost:8000/ws";
        private bool _autoConnect = true;
        private bool _debugMode = false;
        private Vector2 _scrollPosition;
        private string _statusMessage = "";
        private MessageType _statusMessageType = MessageType.Info;
        
        // Windsurf config paths
        private readonly string _windsurfConfigPathMac = "~/Library/Application Support/Windsurf/windsurf_desktop_config.json";
        private readonly string _windsurfConfigPathWindows = "%APPDATA%\\Windsurf\\windsurf_desktop_config.json";
        
        [MenuItem("Window/Windsurf Unity MCP")]
        public static void ShowWindow()
        {
            UnityMcpWindow window = GetWindow<UnityMcpWindow>("Windsurf Unity MCP");
            window.titleContent = new GUIContent("Windsurf Unity MCP");
        }
        
        private void OnEnable()
        {
            // Load settings
            _serverUrl = EditorPrefs.GetString("UnityMcp_ServerUrl", "ws://localhost:8000/ws");
            _autoConnect = EditorPrefs.GetBool("UnityMcp_AutoConnect", true);
            _debugMode = EditorPrefs.GetBool("UnityMcp_DebugMode", false);
        }
        
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            GUILayout.Label("Unity Windsurf MCP Configuration", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            _serverUrl = EditorGUILayout.TextField("Server URL", _serverUrl);
            _autoConnect = EditorGUILayout.Toggle("Auto Connect", _autoConnect);
            _debugMode = EditorGUILayout.Toggle("Debug Mode", _debugMode);
            
            if (EditorGUI.EndChangeCheck())
            {
                // Save settings
                EditorPrefs.SetString("UnityMcp_ServerUrl", _serverUrl);
                EditorPrefs.SetBool("UnityMcp_AutoConnect", _autoConnect);
                EditorPrefs.SetBool("UnityMcp_DebugMode", _debugMode);
                
                // Update the bridge if it exists
                UnityMcpBridge bridge = UnityMcpBridge.Instance;
                if (bridge != null)
                {
                    // We would need to add setters for these properties in the bridge class
                    // bridge.ServerUrl = _serverUrl;
                    // bridge.AutoConnect = _autoConnect;
                    // bridge.DebugMode = _debugMode;
                }
            }
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Connection Status", EditorStyles.boldLabel);
            
            UnityMcpBridge bridge2 = FindObjectOfType<UnityMcpBridge>();
            bool isConnected = bridge2 != null && bridge2.IsConnected;
            
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Status:");
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = isConnected ? Color.green : Color.red;
            GUILayout.Label(isConnected ? "Connected" : "Disconnected", statusStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Connection Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Connect"))
            {
                ConnectToBridge();
            }
            
            if (GUILayout.Button("Disconnect"))
            {
                DisconnectFromBridge();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            GUILayout.Label("MCP Client Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Auto Configure Windsurf"))
            {
                AutoConfigureWindsurf();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusMessageType);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void ConnectToBridge()
        {
            try
            {
                UnityMcpBridge bridge = UnityMcpBridge.Instance;
                if (bridge != null)
                {
                    bridge.Connect();
                    _statusMessage = "Connecting to MCP server...";
                    _statusMessageType = MessageType.Info;
                }
                else
                {
                    _statusMessage = "Failed to get UnityMcpBridge instance";
                    _statusMessageType = MessageType.Error;
                }
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error connecting to MCP server: {ex.Message}";
                _statusMessageType = MessageType.Error;
                Debug.LogError($"[UnityMcpWindow] Error connecting to MCP server: {ex.Message}");
            }
        }
        
        private void DisconnectFromBridge()
        {
            try
            {
                UnityMcpBridge bridge = UnityMcpBridge.Instance;
                if (bridge != null)
                {
                    bridge.Disconnect();
                    _statusMessage = "Disconnected from MCP server";
                    _statusMessageType = MessageType.Info;
                }
                else
                {
                    _statusMessage = "Failed to get UnityMcpBridge instance";
                    _statusMessageType = MessageType.Error;
                }
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error disconnecting from MCP server: {ex.Message}";
                _statusMessageType = MessageType.Error;
                Debug.LogError($"[UnityMcpWindow] Error disconnecting from MCP server: {ex.Message}");
            }
        }
        
        private void AutoConfigureWindsurf()
        {
            try
            {
                string configPath = GetConfigPath(_windsurfConfigPathMac, _windsurfConfigPathWindows);
                
                // Create the config directory if it doesn't exist
                string configDir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                
                // Check if the config file exists
                if (!File.Exists(configPath))
                {
                    // Create an empty config file with default structure
                    File.WriteAllText(configPath, "{\n  \"mcpServers\": {}\n}");
                    Debug.Log($"[UnityMcpWindow] Created new Windsurf config file at {configPath}");
                }
                
                ConfigureMcpClient(configPath, "Windsurf");
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error configuring Windsurf: {ex.Message}";
                _statusMessageType = MessageType.Error;
                Debug.LogError($"[UnityMcpWindow] Error configuring Windsurf: {ex.Message}");
            }
        }
        
        private string GetConfigPath(string macPath, string windowsPath)
        {
            string configPath = "";
            
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                configPath = macPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                configPath = windowsPath.Replace("%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            }
            
            // Return the path regardless of whether the file exists
            return configPath;
        }
        
        private void ConfigureMcpClient(string configPath, string clientName)
        {
            try
            {
                // Read the config file
                string configJson = File.ReadAllText(configPath);
                JObject config = JObject.Parse(configJson);
                
                // Get the server path
                string serverPath = GetServerPath();
                
                // Update the mcpServers section
                JObject mcpServers = config["mcpServers"] as JObject;
                if (mcpServers == null)
                {
                    mcpServers = new JObject();
                    config["mcpServers"] = mcpServers;
                }
                
                // Add or update the Unity Windsurf MCP server
                JObject unityMcp = new JObject
                {
                    ["command"] = "uv",
                    ["args"] = new JArray
                    {
                        "run",
                        "--directory",
                        $"{serverPath}/src",
                        "server.py"
                    }
                };
                
                mcpServers["UnityMCP"] = unityMcp;
                
                // Write the updated config back to the file
                File.WriteAllText(configPath, config.ToString(Formatting.Indented));
                
                _statusMessage = $"{clientName} configured successfully";
                _statusMessageType = MessageType.Info;
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error configuring {clientName}: {ex.Message}";
                _statusMessageType = MessageType.Error;
                Debug.LogError($"[UnityMcpWindow] Error configuring {clientName}: {ex.Message}");
            }
        }
        
        private string GetServerPath()
        {
            // Find the Unity Windsurf MCP Server directory relative to the Unity project
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string parentDir = Directory.GetParent(projectPath).FullName;
            string[] possibleLocations = new string[]
            {
                // Look in the parent directory of the Unity project
                Path.Combine(parentDir, "UnityMcpServer"),
                
                // Look in the same directory as the Unity project
                Path.Combine(projectPath, "UnityMcpServer"),
                
                // Look in the package directory itself
                Path.Combine(projectPath, "Packages", "com.windsurf.unity-mcp", "Server"),
                
                // Look in a directory named WindsurfUnityMCP (new name)
                Path.Combine(parentDir, "WindsurfUnityMCP", "UnityMcpServer"),
                
                // Look in the original repository structure
                Path.Combine(parentDir, "UnityMCP", "UnityMcpServer")
            };
            
            // Check each possible location
            foreach (string path in possibleLocations)
            {
                if (Directory.Exists(path))
                {
                    Debug.Log($"[UnityMcpWindow] Found server at {path}");
                    return path;
                }
            }
            
            // If we can't find it, log an error and suggest manual configuration
            Debug.LogError("[UnityMcpWindow] Could not find UnityMcpServer directory. Please make sure you've downloaded the full repository from https://github.com/isekream/WindsurfUnityMCP and configure the path manually.");
            
            // Return the most likely path as a default
            return Path.Combine(parentDir, "UnityMcpServer");
        }
    }
}
