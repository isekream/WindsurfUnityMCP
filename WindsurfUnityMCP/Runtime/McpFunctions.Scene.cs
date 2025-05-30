using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;

namespace Windsurf.UnityMcp
{
    /// <summary>
    /// Scene management functions for MCP
    /// </summary>
    public static partial class McpFunctions
    {
        /// <summary>
        /// Manage Unity scenes
        /// </summary>
        public static async Task<JObject> ManageSceneAsync(JObject parameters)
        {
            string action = parameters["action"]?.ToString();
            string name = parameters["name"]?.ToString();
            string path = parameters["path"]?.ToString() ?? "Assets/";
            int? buildIndex = parameters["build_index"]?.ToObject<int>();
            
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
                
                // Make sure the path ends with a slash
                if (!path.EndsWith("/"))
                {
                    path += "/";
                }
                
                // Execute on the main thread
                await UnityThreadHelper.RunOnMainThreadAsync(() =>
                {
                    switch (action.ToLower())
                    {
                        case "load":
                            if (string.IsNullOrEmpty(name) && !buildIndex.HasValue)
                            {
                                success = false;
                                message = "Scene name or build index is required";
                                return;
                            }
                            
                            // Check if we need to save the current scene
                            if (EditorSceneManager.GetActiveScene().isDirty)
                            {
                                bool saveResult = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                                if (!saveResult)
                                {
                                    success = false;
                                    message = "Scene load cancelled by user";
                                    return;
                                }
                            }
                            
                            if (buildIndex.HasValue)
                            {
                                // Load by build index
                                if (buildIndex.Value >= 0 && buildIndex.Value < SceneManager.sceneCountInBuildSettings)
                                {
                                    string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex.Value);
                                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                                    success = true;
                                    message = $"Scene at build index {buildIndex.Value} loaded successfully";
                                }
                                else
                                {
                                    success = false;
                                    message = $"Invalid build index: {buildIndex.Value}";
                                }
                            }
                            else
                            {
                                // Load by name
                                string scenePath = $"{path}{name}.unity";
                                if (File.Exists(scenePath))
                                {
                                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                                    success = true;
                                    message = $"Scene '{name}' loaded successfully";
                                }
                                else
                                {
                                    success = false;
                                    message = $"Scene '{name}' not found at path '{scenePath}'";
                                }
                            }
                            break;
                            
                        case "save":
                            Scene currentScene = EditorSceneManager.GetActiveScene();
                            
                            if (string.IsNullOrEmpty(name))
                            {
                                // Save the current scene
                                if (string.IsNullOrEmpty(currentScene.path))
                                {
                                    success = false;
                                    message = "Cannot save scene: Scene has not been saved before";
                                }
                                else
                                {
                                    EditorSceneManager.SaveScene(currentScene);
                                    success = true;
                                    message = "Scene saved successfully";
                                }
                            }
                            else
                            {
                                // Save as a new scene
                                string scenePath = $"{path}{name}.unity";
                                
                                // Create the directory if it doesn't exist
                                Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
                                
                                EditorSceneManager.SaveScene(currentScene, scenePath, true);
                                success = true;
                                message = $"Scene saved as '{name}' successfully";
                            }
                            break;
                            
                        case "create":
                            if (string.IsNullOrEmpty(name))
                            {
                                success = false;
                                message = "Scene name is required";
                                return;
                            }
                            
                            // Check if we need to save the current scene
                            if (EditorSceneManager.GetActiveScene().isDirty)
                            {
                                bool saveResult = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                                if (!saveResult)
                                {
                                    success = false;
                                    message = "Scene creation cancelled by user";
                                    return;
                                }
                            }
                            
                            // Create a new scene
                            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                            
                            // Save the scene
                            string newScenePath = $"{path}{name}.unity";
                            
                            // Create the directory if it doesn't exist
                            Directory.CreateDirectory(Path.GetDirectoryName(newScenePath));
                            
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), newScenePath);
                            success = true;
                            message = $"Scene '{name}' created successfully";
                            break;
                            
                        case "get_hierarchy":
                            Scene scene = EditorSceneManager.GetActiveScene();
                            
                            // Get all root GameObjects in the scene
                            GameObject[] rootObjects = scene.GetRootGameObjects();
                            
                            // Build the hierarchy
                            JArray hierarchy = new JArray();
                            foreach (GameObject rootObject in rootObjects)
                            {
                                hierarchy.Add(BuildGameObjectHierarchy(rootObject));
                            }
                            
                            data = new JObject
                            {
                                ["name"] = scene.name,
                                ["path"] = scene.path,
                                ["isDirty"] = scene.isDirty,
                                ["isLoaded"] = scene.isLoaded,
                                ["rootCount"] = rootObjects.Length,
                                ["hierarchy"] = hierarchy
                            };
                            
                            success = true;
                            message = "Scene hierarchy retrieved";
                            break;
                            
                        case "get_build_settings":
                            int sceneCount = SceneManager.sceneCountInBuildSettings;
                            JArray scenes = new JArray();
                            
                            for (int i = 0; i < sceneCount; i++)
                            {
                                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                                scenes.Add(new JObject
                                {
                                    ["buildIndex"] = i,
                                    ["path"] = scenePath,
                                    ["name"] = Path.GetFileNameWithoutExtension(scenePath)
                                });
                            }
                            
                            data = new JObject
                            {
                                ["sceneCount"] = sceneCount,
                                ["scenes"] = scenes
                            };
                            
                            success = true;
                            message = "Build settings retrieved";
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
        /// Build a hierarchical representation of a GameObject and its children
        /// </summary>
        private static JObject BuildGameObjectHierarchy(GameObject gameObject)
        {
            JObject obj = new JObject
            {
                ["name"] = gameObject.name,
                ["active"] = gameObject.activeSelf,
                ["tag"] = gameObject.tag,
                ["layer"] = LayerMask.LayerToName(gameObject.layer)
            };
            
            // Add components
            JArray components = new JArray();
            Component[] gameObjectComponents = gameObject.GetComponents<Component>();
            foreach (Component component in gameObjectComponents)
            {
                if (component != null) // Some components might be null if scripts are missing
                {
                    components.Add(component.GetType().Name);
                }
            }
            obj["components"] = components;
            
            // Add children
            if (gameObject.transform.childCount > 0)
            {
                JArray children = new JArray();
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    Transform childTransform = gameObject.transform.GetChild(i);
                    children.Add(BuildGameObjectHierarchy(childTransform.gameObject));
                }
                obj["children"] = children;
            }
            
            return obj;
        }
    }
}
