using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Windsurf.UnityMcp
{
    /// <summary>
    /// Console management functions for MCP
    /// </summary>
    public static partial class McpFunctions
    {
        /// <summary>
        /// Get messages from or clear the Unity Editor console
        /// </summary>
        public static async Task<JObject> ReadConsoleAsync(JObject parameters)
        {
            string action = parameters["action"]?.ToString();
            JArray types = parameters["types"] as JArray;
            int? count = parameters["count"]?.ToObject<int>();
            string filterText = parameters["filter_text"]?.ToString();
            string sinceTimestamp = parameters["since_timestamp"]?.ToString();
            string format = parameters["format"]?.ToString() ?? "plain";
            bool includeStacktrace = parameters["include_stacktrace"]?.ToObject<bool>() ?? false;
            
            if (string.IsNullOrEmpty(action))
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = "Action is required"
                };
            }
            
            try
            {
                bool success = false;
                string message = "";
                JObject data = null;
                
                // Execute on the main thread
                await UnityThreadHelper.RunOnMainThreadAsync(() =>
                {
                    switch (action.ToLower())
                    {
                        case "get":
                            // Get console logs using reflection since Unity doesn't expose this API directly
                            var logEntries = GetConsoleLogEntries(types, count, filterText, sinceTimestamp);
                            
                            if (logEntries != null)
                            {
                                JArray logsArray = new JArray();
                                
                                foreach (var entry in logEntries)
                                {
                                    JObject logEntry = new JObject();
                                    
                                    switch (format.ToLower())
                                    {
                                        case "plain":
                                            logEntry["message"] = entry.message;
                                            break;
                                            
                                        case "detailed":
                                            logEntry["message"] = entry.message;
                                            logEntry["type"] = entry.type.ToString();
                                            logEntry["timestamp"] = entry.timestamp.ToString("o");
                                            
                                            if (includeStacktrace && !string.IsNullOrEmpty(entry.stacktrace))
                                            {
                                                logEntry["stacktrace"] = entry.stacktrace;
                                            }
                                            break;
                                            
                                        case "json":
                                            logEntry["message"] = entry.message;
                                            logEntry["type"] = entry.type.ToString();
                                            logEntry["timestamp"] = entry.timestamp.ToString("o");
                                            
                                            if (includeStacktrace && !string.IsNullOrEmpty(entry.stacktrace))
                                            {
                                                logEntry["stacktrace"] = entry.stacktrace;
                                            }
                                            
                                            logEntry["instanceId"] = entry.instanceID;
                                            break;
                                            
                                        default:
                                            logEntry["message"] = entry.message;
                                            break;
                                    }
                                    
                                    logsArray.Add(logEntry);
                                }
                                
                                data = new JObject
                                {
                                    ["logs"] = logsArray,
                                    ["count"] = logsArray.Count
                                };
                                
                                success = true;
                                message = $"Retrieved {logsArray.Count} console messages";
                            }
                            else
                            {
                                success = false;
                                message = "Failed to retrieve console messages";
                            }
                            break;
                            
                        case "clear":
                            // Clear the console using reflection
                            ClearConsole();
                            
                            success = true;
                            message = "Console cleared";
                            break;
                            
                        default:
                            success = false;
                            message = $"Unknown action: {action}";
                            break;
                    }
                });
                
                JObject result = new JObject
                {
                    ["success"] = success,
                    ["message"] = message
                };
                
                if (data != null)
                {
                    result["data"] = data;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Log entry structure
        /// </summary>
        private struct LogEntry
        {
            public string message;
            public string stacktrace;
            public LogType type;
            public DateTime timestamp;
            public int instanceID;
        }
        
        /// <summary>
        /// Get console log entries using reflection
        /// </summary>
        private static List<LogEntry> GetConsoleLogEntries(JArray types, int? count, string filterText, string sinceTimestamp)
        {
            try
            {
                // Get the ConsoleWindow type
                Type consoleWindowType = Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
                if (consoleWindowType == null)
                {
                    Debug.LogError("Could not find ConsoleWindow type");
                    return null;
                }
                
                // Get the LogEntries type
                Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntriesType == null)
                {
                    Debug.LogError("Could not find LogEntries type");
                    return null;
                }
                
                // Get the LogEntry type
                Type logEntryType = Type.GetType("UnityEditor.LogEntry,UnityEditor");
                if (logEntryType == null)
                {
                    Debug.LogError("Could not find LogEntry type");
                    return null;
                }
                
                // Get the StartGettingEntries method
                MethodInfo startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                if (startGettingEntriesMethod == null)
                {
                    Debug.LogError("Could not find StartGettingEntries method");
                    return null;
                }
                
                // Get the GetEntryInternal method
                MethodInfo getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                if (getEntryInternalMethod == null)
                {
                    Debug.LogError("Could not find GetEntryInternal method");
                    return null;
                }
                
                // Get the EndGettingEntries method
                MethodInfo endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);
                if (endGettingEntriesMethod == null)
                {
                    Debug.LogError("Could not find EndGettingEntries method");
                    return null;
                }
                
                // Get the GetCount method
                MethodInfo getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                if (getCountMethod == null)
                {
                    Debug.LogError("Could not find GetCount method");
                    return null;
                }
                
                // Get the count of log entries
                int entryCount = (int)getCountMethod.Invoke(null, null);
                
                // Create a list to store the log entries
                List<LogEntry> logEntries = new List<LogEntry>();
                
                // Start getting entries
                startGettingEntriesMethod.Invoke(null, null);
                
                // Create an instance of LogEntry to pass to GetEntryInternal
                object logEntryInstance = Activator.CreateInstance(logEntryType);
                
                // Parse the since timestamp
                DateTime? since = null;
                if (!string.IsNullOrEmpty(sinceTimestamp))
                {
                    if (DateTime.TryParse(sinceTimestamp, out DateTime parsedTimestamp))
                    {
                        since = parsedTimestamp;
                    }
                }
                
                // Get the entries
                for (int i = 0; i < entryCount; i++)
                {
                    // Get the entry
                    getEntryInternalMethod.Invoke(null, new object[] { i, logEntryInstance });
                    
                    // Get the fields from the entry
                    string entryMessage = (string)logEntryType.GetField("message").GetValue(logEntryInstance);
                    string entryStacktrace = (string)logEntryType.GetField("stacktrace").GetValue(logEntryInstance);
                    int entryType = (int)logEntryType.GetField("mode").GetValue(logEntryInstance);
                    int entryInstanceID = (int)logEntryType.GetField("instanceID").GetValue(logEntryInstance);
                    
                    // Convert the type to LogType
                    LogType logType = LogType.Log;
                    switch (entryType)
                    {
                        case 0:
                            logType = LogType.Error;
                            break;
                        case 1:
                            logType = LogType.Assert;
                            break;
                        case 2:
                            logType = LogType.Warning;
                            break;
                        case 3:
                            logType = LogType.Log;
                            break;
                        case 4:
                            logType = LogType.Exception;
                            break;
                    }
                    
                    // Filter by type if specified
                    if (types != null && types.Count > 0)
                    {
                        bool typeMatches = false;
                        foreach (JToken typeToken in types)
                        {
                            string typeStr = typeToken.ToString().ToLower();
                            
                            if (typeStr == "all" ||
                                (typeStr == "error" && (logType == LogType.Error || logType == LogType.Exception)) ||
                                (typeStr == "warning" && logType == LogType.Warning) ||
                                (typeStr == "log" && logType == LogType.Log))
                            {
                                typeMatches = true;
                                break;
                            }
                        }
                        
                        if (!typeMatches)
                        {
                            continue;
                        }
                    }
                    
                    // Filter by text if specified
                    if (!string.IsNullOrEmpty(filterText) && !entryMessage.Contains(filterText))
                    {
                        continue;
                    }
                    
                    // Create a timestamp (approximate since Unity doesn't store this)
                    DateTime timestamp = DateTime.Now.AddSeconds(-1 * (entryCount - i));
                    
                    // Filter by timestamp if specified
                    if (since.HasValue && timestamp < since.Value)
                    {
                        continue;
                    }
                    
                    // Add the entry to the list
                    logEntries.Add(new LogEntry
                    {
                        message = entryMessage,
                        stacktrace = entryStacktrace,
                        type = logType,
                        timestamp = timestamp,
                        instanceID = entryInstanceID
                    });
                }
                
                // End getting entries
                endGettingEntriesMethod.Invoke(null, null);
                
                // Limit the number of entries if specified
                if (count.HasValue && count.Value > 0 && logEntries.Count > count.Value)
                {
                    logEntries = logEntries.Skip(logEntries.Count - count.Value).ToList();
                }
                
                return logEntries;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting console log entries: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Clear the console using reflection
        /// </summary>
        private static void ClearConsole()
        {
            try
            {
                // Get the LogEntries type
                Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntriesType == null)
                {
                    Debug.LogError("Could not find LogEntries type");
                    return;
                }
                
                // Get the Clear method
                MethodInfo clearMethod = logEntriesType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                if (clearMethod == null)
                {
                    Debug.LogError("Could not find Clear method");
                    return;
                }
                
                // Invoke the Clear method
                clearMethod.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error clearing console: {ex.Message}");
            }
        }
    }
}
