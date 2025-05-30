using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace Windsurf.UnityMcp
{
    /// <summary>
    /// Main bridge class that connects Unity to the MCP server
    /// </summary>
    public class UnityMcpBridge : MonoBehaviour
    {
        private static UnityMcpBridge _instance;
        
        [SerializeField] private string _serverUrl = "ws://localhost:8000/ws";
        [SerializeField] private bool _autoConnect = true;
        [SerializeField] private bool _debugMode = false;
        
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected = false;
        private Dictionary<string, Func<JObject, Task<JObject>>> _functionHandlers;
        
        // Events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        
        public static UnityMcpBridge Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("UnityMcpBridge");
                    _instance = go.AddComponent<UnityMcpBridge>();
                    
                    // Only use DontDestroyOnLoad in play mode
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        public bool IsConnected => _isConnected;
        public string ServerUrl => _serverUrl;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeFunctionHandlers();
        }
        
        private void Start()
        {
            if (_autoConnect)
            {
                Connect();
            }
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        private void InitializeFunctionHandlers()
        {
            _functionHandlers = new Dictionary<string, Func<JObject, Task<JObject>>>
            {
                { "execute_menu_item", McpFunctions.ExecuteMenuItemAsync },
                { "manage_script", McpFunctions.ManageScriptAsync },
                { "manage_editor", McpFunctions.ManageEditorAsync },
                { "manage_scene", McpFunctions.ManageSceneAsync },
                { "manage_asset", McpFunctions.ManageAssetAsync },
                { "manage_gameobject", McpFunctions.ManageGameObjectAsync },
                { "read_console", McpFunctions.ReadConsoleAsync }
            };
        }
        
        /// <summary>
        /// Connect to the MCP server
        /// </summary>
        public async void Connect()
        {
            if (_isConnected)
            {
                Debug.Log("[UnityMcpBridge] Already connected to the server.");
                return;
            }
            
            try
            {
                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();
                
                Debug.Log($"[UnityMcpBridge] Connecting to {_serverUrl}...");
                await _webSocket.ConnectAsync(new Uri(_serverUrl), _cancellationTokenSource.Token);
                
                _isConnected = true;
                Debug.Log("[UnityMcpBridge] Connected to the server.");
                
                // Send initial message to identify as Unity
                var initialMessage = new JObject
                {
                    ["client_type"] = "unity",
                    ["unity_version"] = Application.unityVersion
                };
                
                await SendMessageAsync(initialMessage);
                
                // Start receiving messages
                _ = ReceiveMessagesAsync();
                
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityMcpBridge] Connection error: {ex.Message}");
                OnError?.Invoke($"Connection error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Disconnect from the MCP server
        /// </summary>
        public async void Disconnect()
        {
            if (!_isConnected)
            {
                return;
            }
            
            try
            {
                if (_webSocket != null)
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                    }
                    
                    _webSocket.Dispose();
                    _webSocket = null;
                }
                
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                
                _isConnected = false;
                Debug.Log("[UnityMcpBridge] Disconnected from the server.");
                
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityMcpBridge] Disconnection error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send a message to the MCP server
        /// </summary>
        private async Task SendMessageAsync(JObject message)
        {
            if (!_isConnected || _webSocket == null)
            {
                Debug.LogWarning("[UnityMcpBridge] Cannot send message: Not connected to the server.");
                return;
            }
            
            try
            {
                string json = message.ToString(Formatting.None);
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                
                if (_debugMode)
                {
                    Debug.Log($"[UnityMcpBridge] Sending: {json}");
                }
                
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityMcpBridge] Send error: {ex.Message}");
                OnError?.Invoke($"Send error: {ex.Message}");
                
                // Try to reconnect
                Disconnect();
                Connect();
            }
        }
        
        /// <summary>
        /// Continuously receive messages from the MCP server
        /// </summary>
        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[8192];
            
            try
            {
                while (_isConnected && _webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        _isConnected = false;
                        OnDisconnected?.Invoke();
                        break;
                    }
                    
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    
                    if (_debugMode)
                    {
                        Debug.Log($"[UnityMcpBridge] Received: {message}");
                    }
                    
                    // Process the message
                    _ = ProcessMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    Debug.Log("[UnityMcpBridge] WebSocket receive operation canceled.");
                }
                else
                {
                    Debug.LogError($"[UnityMcpBridge] Receive error: {ex.Message}");
                    OnError?.Invoke($"Receive error: {ex.Message}");
                }
                
                _isConnected = false;
                OnDisconnected?.Invoke();
            }
        }
        
        /// <summary>
        /// Process a message received from the MCP server
        /// </summary>
        private async Task ProcessMessageAsync(string jsonMessage)
        {
            try
            {
                JObject message = JObject.Parse(jsonMessage);
                
                // Get the function name and parameters
                string function = message["function"]?.ToString();
                string id = message["id"]?.ToString();
                
                if (string.IsNullOrEmpty(function) || string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning("[UnityMcpBridge] Received message with missing function or id.");
                    return;
                }
                
                // Check if we have a handler for this function
                if (_functionHandlers.TryGetValue(function, out var handler))
                {
                    try
                    {
                        // Execute the function
                        JObject result = await handler(message["params"] as JObject);
                        
                        // Add the id to the result
                        result["id"] = id;
                        
                        // Send the result back to the server
                        await SendMessageAsync(result);
                    }
                    catch (Exception ex)
                    {
                        // Send error response
                        JObject errorResponse = new JObject
                        {
                            ["id"] = id,
                            ["success"] = false,
                            ["error"] = ex.Message
                        };
                        
                        await SendMessageAsync(errorResponse);
                    }
                }
                else
                {
                    // Unknown function
                    JObject errorResponse = new JObject
                    {
                        ["id"] = id,
                        ["success"] = false,
                        ["error"] = $"Unknown function: {function}"
                    };
                    
                    await SendMessageAsync(errorResponse);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityMcpBridge] Error processing message: {ex.Message}");
            }
        }
    }
}
