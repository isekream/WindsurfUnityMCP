using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;

namespace Windsurf.UnityMcp
{
    /// <summary>
    /// Contains all MCP function implementations for Unity operations
    /// </summary>
    public static class McpFunctions
    {
        /// <summary>
        /// Execute a Unity Editor menu item
        /// </summary>
        public static async Task<JObject> ExecuteMenuItemAsync(JObject parameters)
        {
            string menuPath = parameters["menu_path"]?.ToString();
            string action = parameters["action"]?.ToString() ?? "execute";
            JObject menuParams = parameters["parameters"] as JObject;
            
            if (string.IsNullOrEmpty(menuPath))
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = "Menu path is required"
                };
            }
            
            try
            {
                bool success = false;
                string message = "";
                
                // Execute on the main thread
                await UnityThreadHelper.RunOnMainThreadAsync(() =>
                {
                    if (action == "execute")
                    {
                        // Check if the menu item exists
                        if (EditorApplication.ExecuteMenuItem(menuPath))
                        {
                            success = true;
                            message = $"Menu item '{menuPath}' executed successfully";
                        }
                        else
                        {
                            success = false;
                            message = $"Menu item '{menuPath}' not found";
                        }
                    }
                    else
                    {
                        success = false;
                        message = $"Unknown action: {action}";
                    }
                });
                
                return new JObject
                {
                    ["success"] = success,
                    ["message"] = message
                };
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
        /// Manage C# scripts in Unity
        /// </summary>
        public static async Task<JObject> ManageScriptAsync(JObject parameters)
        {
            string action = parameters["action"]?.ToString();
            string name = parameters["name"]?.ToString();
            string path = parameters["path"]?.ToString() ?? "Assets/";
            string contents = parameters["contents"]?.ToString();
            string scriptType = parameters["script_type"]?.ToString() ?? "MonoBehaviour";
            string namespaceName = parameters["namespace"]?.ToString();
            
            if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(name))
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = "Action and name are required"
                };
            }
            
            try
            {
                bool success = false;
                string message = "";
                string scriptContent = "";
                
                // Make sure the path ends with a slash
                if (!path.EndsWith("/"))
                {
                    path += "/";
                }
                
                // Full path to the script
                string fullPath = $"{path}{name}.cs";
                
                // Execute on the main thread
                await UnityThreadHelper.RunOnMainThreadAsync(() =>
                {
                    switch (action.ToLower())
                    {
                        case "create":
                            if (string.IsNullOrEmpty(contents))
                            {
                                // Generate default script content
                                contents = GenerateDefaultScript(name, scriptType, namespaceName);
                            }
                            
                            // Create the directory if it doesn't exist
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            
                            // Write the script
                            File.WriteAllText(fullPath, contents);
                            
                            // Refresh the asset database
                            AssetDatabase.Refresh();
                            
                            success = true;
                            message = $"Script '{name}' created successfully";
                            break;
                            
                        case "read":
                            if (File.Exists(fullPath))
                            {
                                scriptContent = File.ReadAllText(fullPath);
                                success = true;
                                message = $"Script '{name}' read successfully";
                            }
                            else
                            {
                                success = false;
                                message = $"Script '{name}' not found";
                            }
                            break;
                            
                        case "update":
                            if (File.Exists(fullPath))
                            {
                                if (string.IsNullOrEmpty(contents))
                                {
                                    success = false;
                                    message = "Contents are required for update";
                                }
                                else
                                {
                                    File.WriteAllText(fullPath, contents);
                                    AssetDatabase.Refresh();
                                    success = true;
                                    message = $"Script '{name}' updated successfully";
                                }
                            }
                            else
                            {
                                success = false;
                                message = $"Script '{name}' not found";
                            }
                            break;
                            
                        case "delete":
                            if (File.Exists(fullPath))
                            {
                                AssetDatabase.DeleteAsset(fullPath);
                                success = true;
                                message = $"Script '{name}' deleted successfully";
                            }
                            else
                            {
                                success = false;
                                message = $"Script '{name}' not found";
                            }
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
                
                if (action.ToLower() == "read" && success)
                {
                    result["data"] = scriptContent;
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
        /// Generate default script content
        /// </summary>
        private static string GenerateDefaultScript(string className, string scriptType, string namespaceName)
        {
            string namespaceStart = string.IsNullOrEmpty(namespaceName) ? "" : $"namespace {namespaceName}\n{{\n";
            string namespaceEnd = string.IsNullOrEmpty(namespaceName) ? "" : "}\n";
            string indent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";
            
            string baseClass = scriptType.ToLower() switch
            {
                "monobehaviour" => "MonoBehaviour",
                "scriptableobject" => "ScriptableObject",
                "editor" => "Editor",
                "editorwindow" => "EditorWindow",
                _ => scriptType
            };
            
            return $"using System;\nusing System.Collections;\nusing System.Collections.Generic;\nusing UnityEngine;\n\n{namespaceStart}{indent}/// <summary>\n{indent}/// {className} class\n{indent}/// </summary>\n{indent}public class {className} : {baseClass}\n{indent}{{\n{indent}    // Start is called before the first frame update\n{indent}    private void Start()\n{indent}    {{\n{indent}        \n{indent}    }}\n\n{indent}    // Update is called once per frame\n{indent}    private void Update()\n{indent}    {{\n{indent}        \n{indent}    }}\n{indent}}}\n{namespaceEnd}";
        }
        
        /// <summary>
        /// Control and query the Unity editor's state and settings
        /// </summary>
        public static async Task<JObject> ManageEditorAsync(JObject parameters)
        {
            string action = parameters["action"]?.ToString();
            bool? waitForCompletion = parameters["wait_for_completion"]?.ToObject<bool>();
            string toolName = parameters["tool_name"]?.ToString();
            string tagName = parameters["tag_name"]?.ToString();
            string layerName = parameters["layer_name"]?.ToString();
            
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
                        case "play":
                            if (!EditorApplication.isPlaying)
                            {
                                EditorApplication.isPlaying = true;
                                success = true;
                                message = "Play mode started";
                            }
                            else
                            {
                                success = true;
                                message = "Already in play mode";
                            }
                            break;
                            
                        case "pause":
                            if (EditorApplication.isPlaying)
                            {
                                EditorApplication.isPaused = !EditorApplication.isPaused;
                                success = true;
                                message = EditorApplication.isPaused ? "Game paused" : "Game resumed";
                            }
                            else
                            {
                                success = false;
                                message = "Not in play mode";
                            }
                            break;
                            
                        case "stop":
                            if (EditorApplication.isPlaying)
                            {
                                EditorApplication.isPlaying = false;
                                success = true;
                                message = "Play mode stopped";
                            }
                            else
                            {
                                success = true;
                                message = "Not in play mode";
                            }
                            break;
                            
                        case "get_state":
                            data = new JObject
                            {
                                ["isPlaying"] = EditorApplication.isPlaying,
                                ["isPaused"] = EditorApplication.isPaused,
                                ["isCompiling"] = EditorApplication.isCompiling,
                                ["isUpdating"] = EditorApplication.isUpdating
                            };
                            success = true;
                            message = "Editor state retrieved";
                            break;
                            
                        case "add_tag":
                            if (string.IsNullOrEmpty(tagName))
                            {
                                success = false;
                                message = "Tag name is required";
                            }
                            else
                            {
                                // Get the tags from the project settings
                                SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                                SerializedProperty tagsProp = tagManager.FindProperty("tags");
                                
                                // Check if the tag already exists
                                bool tagExists = false;
                                for (int i = 0; i < tagsProp.arraySize; i++)
                                {
                                    SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
                                    if (tag.stringValue == tagName)
                                    {
                                        tagExists = true;
                                        break;
                                    }
                                }
                                
                                if (tagExists)
                                {
                                    success = true;
                                    message = $"Tag '{tagName}' already exists";
                                }
                                else
                                {
                                    // Add the tag
                                    tagsProp.arraySize++;
                                    SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                                    newTag.stringValue = tagName;
                                    
                                    // Save the changes
                                    tagManager.ApplyModifiedProperties();
                                    
                                    success = true;
                                    message = $"Tag '{tagName}' added successfully";
                                }
                            }
                            break;
                            
                        case "add_layer":
                            if (string.IsNullOrEmpty(layerName))
                            {
                                success = false;
                                message = "Layer name is required";
                            }
                            else
                            {
                                // Get the layers from the project settings
                                SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                                SerializedProperty layersProp = tagManager.FindProperty("layers");
                                
                                // Find the first empty layer slot
                                int emptySlot = -1;
                                for (int i = 8; i < layersProp.arraySize; i++) // User layers start at index 8
                                {
                                    SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                                    if (string.IsNullOrEmpty(layerProp.stringValue))
                                    {
                                        emptySlot = i;
                                        break;
                                    }
                                    else if (layerProp.stringValue == layerName)
                                    {
                                        success = true;
                                        message = $"Layer '{layerName}' already exists at index {i}";
                                        return;
                                    }
                                }
                                
                                if (emptySlot != -1)
                                {
                                    // Add the layer
                                    SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(emptySlot);
                                    layerProp.stringValue = layerName;
                                    
                                    // Save the changes
                                    tagManager.ApplyModifiedProperties();
                                    
                                    success = true;
                                    message = $"Layer '{layerName}' added successfully at index {emptySlot}";
                                }
                                else
                                {
                                    success = false;
                                    message = "No empty layer slots available";
                                }
                            }
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
        /// Manage Unity scenes
        /// </summary>
        public static async Task<JObject> ManageSceneAsync(JObject parameters)
        {
            // Implementation will be added in a separate file
            return new JObject { ["success"] = false, ["error"] = "Not implemented yet" };
        }
        
        /// <summary>
        /// Perform asset operations in Unity
        /// </summary>
        public static async Task<JObject> ManageAssetAsync(JObject parameters)
        {
            // Implementation will be added in a separate file
            return new JObject { ["success"] = false, ["error"] = "Not implemented yet" };
        }
        
        /// <summary>
        /// Manage GameObjects in Unity
        /// </summary>
        public static async Task<JObject> ManageGameObjectAsync(JObject parameters)
        {
            // Implementation will be added in a separate file
            return new JObject { ["success"] = false, ["error"] = "Not implemented yet" };
        }
        
        /// <summary>
        /// Get messages from or clear the Unity Editor console
        /// </summary>
        public static async Task<JObject> ReadConsoleAsync(JObject parameters)
        {
            // Implementation will be added in a separate file
            return new JObject { ["success"] = false, ["error"] = "Not implemented yet" };
        }
    }
}
