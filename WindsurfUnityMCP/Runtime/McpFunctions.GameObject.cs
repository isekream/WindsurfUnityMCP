using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using System.Reflection;

namespace Windsurf.UnityMcp
{
    /// <summary>
    /// GameObject management functions for MCP
    /// </summary>
    public static partial class McpFunctions
    {
        /// <summary>
        /// Manage GameObjects in Unity
        /// </summary>
        public static async Task<JObject> ManageGameObjectAsync(JObject parameters)
        {
            string action = parameters["action"]?.ToString();
            string target = parameters["target"]?.ToString();
            string searchMethod = parameters["search_method"]?.ToString() ?? "by_name";
            string name = parameters["name"]?.ToString();
            string tag = parameters["tag"]?.ToString();
            string parent = parameters["parent"]?.ToString();
            string layer = parameters["layer"]?.ToString();
            JArray position = parameters["position"] as JArray;
            JArray rotation = parameters["rotation"] as JArray;
            JArray scale = parameters["scale"] as JArray;
            string componentName = parameters["component_name"]?.ToString();
            JObject componentProperties = parameters["component_properties"] as JObject;
            JArray componentsToAdd = parameters["components_to_add"] as JArray;
            JArray componentsToRemove = parameters["components_to_remove"] as JArray;
            string searchTerm = parameters["search_term"]?.ToString();
            bool searchInChildren = parameters["search_in_children"]?.ToObject<bool>() ?? false;
            bool searchInactive = parameters["search_inactive"]?.ToObject<bool>() ?? false;
            bool findAll = parameters["find_all"]?.ToObject<bool>() ?? false;
            bool? setActive = parameters["set_active"]?.ToObject<bool>();
            string primitiveType = parameters["primitive_type"]?.ToString();
            string prefabPath = parameters["prefab_path"]?.ToString();
            bool saveAsPrefab = parameters["save_as_prefab"]?.ToObject<bool>() ?? false;
            string prefabFolder = parameters["prefab_folder"]?.ToString() ?? "Assets/Prefabs";
            
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
                        case "create":
                            GameObject newObject = null;
                            
                            // Create from primitive
                            if (!string.IsNullOrEmpty(primitiveType))
                            {
                                if (Enum.TryParse<PrimitiveType>(primitiveType, true, out PrimitiveType type))
                                {
                                    newObject = GameObject.CreatePrimitive(type);
                                }
                                else
                                {
                                    success = false;
                                    message = $"Invalid primitive type: {primitiveType}";
                                    return;
                                }
                            }
                            // Create from prefab
                            else if (!string.IsNullOrEmpty(prefabPath))
                            {
                                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                                if (prefab != null)
                                {
                                    newObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                                }
                                else
                                {
                                    success = false;
                                    message = $"Prefab not found at path: {prefabPath}";
                                    return;
                                }
                            }
                            // Create empty GameObject
                            else
                            {
                                newObject = new GameObject();
                            }
                            
                            // Set name
                            if (!string.IsNullOrEmpty(name))
                            {
                                newObject.name = name;
                            }
                            
                            // Set tag
                            if (!string.IsNullOrEmpty(tag))
                            {
                                newObject.tag = tag;
                            }
                            
                            // Set layer
                            if (!string.IsNullOrEmpty(layer))
                            {
                                newObject.layer = LayerMask.NameToLayer(layer);
                            }
                            
                            // Set parent
                            if (!string.IsNullOrEmpty(parent))
                            {
                                GameObject parentObject = FindGameObject(parent, "by_name", false, false);
                                if (parentObject != null)
                                {
                                    newObject.transform.SetParent(parentObject.transform, false);
                                }
                            }
                            
                            // Set position
                            if (position != null && position.Count == 3)
                            {
                                newObject.transform.position = new Vector3(
                                    position[0].ToObject<float>(),
                                    position[1].ToObject<float>(),
                                    position[2].ToObject<float>()
                                );
                            }
                            
                            // Set rotation
                            if (rotation != null && rotation.Count == 3)
                            {
                                newObject.transform.rotation = Quaternion.Euler(
                                    rotation[0].ToObject<float>(),
                                    rotation[1].ToObject<float>(),
                                    rotation[2].ToObject<float>()
                                );
                            }
                            
                            // Set scale
                            if (scale != null && scale.Count == 3)
                            {
                                newObject.transform.localScale = new Vector3(
                                    scale[0].ToObject<float>(),
                                    scale[1].ToObject<float>(),
                                    scale[2].ToObject<float>()
                                );
                            }
                            
                            // Add components
                            if (componentsToAdd != null)
                            {
                                foreach (JToken componentToken in componentsToAdd)
                                {
                                    string componentToAdd = componentToken.ToString();
                                    AddComponentToGameObject(newObject, componentToAdd);
                                }
                            }
                            
                            // Set component properties
                            if (componentProperties != null)
                            {
                                SetComponentProperties(newObject, componentProperties);
                            }
                            
                            // Save as prefab
                            if (saveAsPrefab)
                            {
                                // Create the prefab folder if it doesn't exist
                                if (!Directory.Exists(prefabFolder))
                                {
                                    Directory.CreateDirectory(prefabFolder);
                                }
                                
                                string prefabName = !string.IsNullOrEmpty(name) ? name : newObject.name;
                                string newPrefabPath = $"{prefabFolder}/{prefabName}.prefab";
                                
                                PrefabUtility.SaveAsPrefabAsset(newObject, newPrefabPath);
                            }
                            
                            success = true;
                            message = $"GameObject created successfully: {newObject.name}";
                            break;
                            
                        case "find":
                            if (string.IsNullOrEmpty(searchTerm))
                            {
                                success = false;
                                message = "Search term is required";
                                return;
                            }
                            
                            if (findAll)
                            {
                                // Find all matching GameObjects
                                GameObject[] foundObjects = FindGameObjects(searchTerm, searchMethod, searchInChildren, searchInactive);
                                
                                if (foundObjects.Length > 0)
                                {
                                    JArray objectsArray = new JArray();
                                    foreach (GameObject obj in foundObjects)
                                    {
                                        objectsArray.Add(GameObjectToJObject(obj));
                                    }
                                    
                                    data = new JObject
                                    {
                                        ["count"] = foundObjects.Length,
                                        ["objects"] = objectsArray
                                    };
                                    
                                    success = true;
                                    message = $"Found {foundObjects.Length} GameObjects matching '{searchTerm}'";
                                }
                                else
                                {
                                    success = false;
                                    message = $"No GameObjects found matching '{searchTerm}'";
                                }
                            }
                            else
                            {
                                // Find a single GameObject
                                GameObject foundObject = FindGameObject(searchTerm, searchMethod, searchInChildren, searchInactive);
                                
                                if (foundObject != null)
                                {
                                    data = GameObjectToJObject(foundObject);
                                    success = true;
                                    message = $"GameObject '{foundObject.name}' found";
                                }
                                else
                                {
                                    success = false;
                                    message = $"GameObject not found: {searchTerm}";
                                }
                            }
                            break;
                            
                        case "modify":
                            if (string.IsNullOrEmpty(target))
                            {
                                success = false;
                                message = "Target GameObject is required";
                                return;
                            }
                            
                            GameObject targetObject = FindGameObject(target, searchMethod, false, false);
                            
                            if (targetObject != null)
                            {
                                // Set name
                                if (!string.IsNullOrEmpty(name))
                                {
                                    targetObject.name = name;
                                }
                                
                                // Set tag
                                if (!string.IsNullOrEmpty(tag))
                                {
                                    targetObject.tag = tag;
                                }
                                
                                // Set layer
                                if (!string.IsNullOrEmpty(layer))
                                {
                                    targetObject.layer = LayerMask.NameToLayer(layer);
                                }
                                
                                // Set parent
                                if (!string.IsNullOrEmpty(parent))
                                {
                                    GameObject parentObject = FindGameObject(parent, "by_name", false, false);
                                    if (parentObject != null)
                                    {
                                        targetObject.transform.SetParent(parentObject.transform, true);
                                    }
                                }
                                
                                // Set position
                                if (position != null && position.Count == 3)
                                {
                                    targetObject.transform.position = new Vector3(
                                        position[0].ToObject<float>(),
                                        position[1].ToObject<float>(),
                                        position[2].ToObject<float>()
                                    );
                                }
                                
                                // Set rotation
                                if (rotation != null && rotation.Count == 3)
                                {
                                    targetObject.transform.rotation = Quaternion.Euler(
                                        rotation[0].ToObject<float>(),
                                        rotation[1].ToObject<float>(),
                                        rotation[2].ToObject<float>()
                                    );
                                }
                                
                                // Set scale
                                if (scale != null && scale.Count == 3)
                                {
                                    targetObject.transform.localScale = new Vector3(
                                        scale[0].ToObject<float>(),
                                        scale[1].ToObject<float>(),
                                        scale[2].ToObject<float>()
                                    );
                                }
                                
                                // Set active state
                                if (setActive.HasValue)
                                {
                                    targetObject.SetActive(setActive.Value);
                                }
                                
                                // Add components
                                if (componentsToAdd != null)
                                {
                                    foreach (JToken componentToken in componentsToAdd)
                                    {
                                        string componentToAdd = componentToken.ToString();
                                        AddComponentToGameObject(targetObject, componentToAdd);
                                    }
                                }
                                
                                // Remove components
                                if (componentsToRemove != null)
                                {
                                    foreach (JToken componentToken in componentsToRemove)
                                    {
                                        string componentToRemove = componentToken.ToString();
                                        RemoveComponentFromGameObject(targetObject, componentToRemove);
                                    }
                                }
                                
                                // Set component properties
                                if (componentProperties != null)
                                {
                                    SetComponentProperties(targetObject, componentProperties);
                                }
                                
                                success = true;
                                message = $"GameObject '{targetObject.name}' modified successfully";
                            }
                            else
                            {
                                success = false;
                                message = $"GameObject not found: {target}";
                            }
                            break;
                            
                        case "delete":
                            if (string.IsNullOrEmpty(target))
                            {
                                success = false;
                                message = "Target GameObject is required";
                                return;
                            }
                            
                            GameObject objectToDelete = FindGameObject(target, searchMethod, false, false);
                            
                            if (objectToDelete != null)
                            {
                                UnityEngine.Object.DestroyImmediate(objectToDelete);
                                success = true;
                                message = $"GameObject '{target}' deleted successfully";
                            }
                            else
                            {
                                success = false;
                                message = $"GameObject not found: {target}";
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
        /// Find a GameObject by name, path, or tag
        /// </summary>
        private static GameObject FindGameObject(string search, string method, bool searchInChildren, bool searchInactive)
        {
            switch (method.ToLower())
            {
                case "by_name":
                    return GameObject.Find(search);
                    
                case "by_tag":
                    return GameObject.FindWithTag(search);
                    
                case "by_path":
                    Transform transform = GameObject.Find(search.Split('/')[0])?.transform;
                    
                    if (transform == null)
                    {
                        return null;
                    }
                    
                    string[] pathParts = search.Split('/');
                    
                    for (int i = 1; i < pathParts.Length; i++)
                    {
                        transform = transform.Find(pathParts[i]);
                        
                        if (transform == null)
                        {
                            return null;
                        }
                    }
                    
                    return transform.gameObject;
                    
                default:
                    return GameObject.Find(search);
            }
        }
        
        /// <summary>
        /// Find all GameObjects matching the search criteria
        /// </summary>
        private static GameObject[] FindGameObjects(string search, string method, bool searchInChildren, bool searchInactive)
        {
            switch (method.ToLower())
            {
                case "by_name":
                    List<GameObject> results = new List<GameObject>();
                    GameObject[] allObjects = searchInactive ? Resources.FindObjectsOfTypeAll<GameObject>() : UnityEngine.Object.FindObjectsOfType<GameObject>();
                    
                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.name == search)
                        {
                            results.Add(obj);
                        }
                        else if (searchInChildren)
                        {
                            Transform[] childTransforms = obj.GetComponentsInChildren<Transform>(searchInactive);
                            
                            foreach (Transform child in childTransforms)
                            {
                                if (child.gameObject.name == search && !results.Contains(child.gameObject))
                                {
                                    results.Add(child.gameObject);
                                }
                            }
                        }
                    }
                    
                    return results.ToArray();
                    
                case "by_tag":
                    return GameObject.FindGameObjectsWithTag(search);
                    
                default:
                    return GameObject.FindObjectsOfType<GameObject>().Where(obj => obj.name == search).ToArray();
            }
        }
        
        /// <summary>
        /// Convert a GameObject to a JObject
        /// </summary>
        private static JObject GameObjectToJObject(GameObject gameObject)
        {
            JObject obj = new JObject
            {
                ["name"] = gameObject.name,
                ["active"] = gameObject.activeSelf,
                ["tag"] = gameObject.tag,
                ["layer"] = LayerMask.LayerToName(gameObject.layer),
                ["position"] = new JArray { gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z },
                ["rotation"] = new JArray { gameObject.transform.eulerAngles.x, gameObject.transform.eulerAngles.y, gameObject.transform.eulerAngles.z },
                ["scale"] = new JArray { gameObject.transform.localScale.x, gameObject.transform.localScale.y, gameObject.transform.localScale.z }
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
            
            return obj;
        }
        
        /// <summary>
        /// Add a component to a GameObject
        /// </summary>
        private static void AddComponentToGameObject(GameObject gameObject, string componentName)
        {
            // Try to find the component type
            Type componentType = null;
            
            // First check built-in components
            componentType = Type.GetType($"UnityEngine.{componentName}, UnityEngine");
            
            // If not found, check all loaded assemblies
            if (componentType == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    componentType = assembly.GetType(componentName);
                    if (componentType != null)
                    {
                        break;
                    }
                }
            }
            
            // If still not found, try to find by name
            if (componentType == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.Name == componentName && typeof(Component).IsAssignableFrom(type))
                        {
                            componentType = type;
                            break;
                        }
                    }
                    
                    if (componentType != null)
                    {
                        break;
                    }
                }
            }
            
            // Add the component
            if (componentType != null)
            {
                gameObject.AddComponent(componentType);
            }
        }
        
        /// <summary>
        /// Remove a component from a GameObject
        /// </summary>
        private static void RemoveComponentFromGameObject(GameObject gameObject, string componentName)
        {
            Component component = null;
            
            // Try to find the component by name
            Component[] components = gameObject.GetComponents<Component>();
            foreach (Component c in components)
            {
                if (c.GetType().Name == componentName)
                {
                    component = c;
                    break;
                }
            }
            
            // Remove the component
            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }
        
        /// <summary>
        /// Set properties on components of a GameObject
        /// </summary>
        private static void SetComponentProperties(GameObject gameObject, JObject componentProperties)
        {
            foreach (KeyValuePair<string, JToken> componentProp in componentProperties)
            {
                string componentName = componentProp.Key;
                JObject properties = componentProp.Value as JObject;
                
                if (properties == null)
                {
                    continue;
                }
                
                // Find the component
                Component component = null;
                Component[] components = gameObject.GetComponents<Component>();
                foreach (Component c in components)
                {
                    if (c.GetType().Name == componentName)
                    {
                        component = c;
                        break;
                    }
                }
                
                if (component == null)
                {
                    // Try to add the component
                    AddComponentToGameObject(gameObject, componentName);
                    
                    // Try to find it again
                    components = gameObject.GetComponents<Component>();
                    foreach (Component c in components)
                    {
                        if (c.GetType().Name == componentName)
                        {
                            component = c;
                            break;
                        }
                    }
                    
                    if (component == null)
                    {
                        // Still not found, skip this component
                        continue;
                    }
                }
                
                // Set the properties
                foreach (KeyValuePair<string, JToken> property in properties)
                {
                    SetComponentProperty(component, property.Key, property.Value);
                }
            }
        }
        
        /// <summary>
        /// Set a property on a component
        /// </summary>
        private static void SetComponentProperty(Component component, string propertyPath, JToken value)
        {
            try
            {
                Type componentType = component.GetType();
                
                // Handle nested properties
                string[] propertyParts = propertyPath.Split('.');
                object target = component;
                
                for (int i = 0; i < propertyParts.Length - 1; i++)
                {
                    string part = propertyParts[i];
                    
                    // Get the field or property
                    FieldInfo field = target.GetType().GetField(part, BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo property = target.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        target = field.GetValue(target);
                    }
                    else if (property != null)
                    {
                        target = property.GetValue(target);
                    }
                    else
                    {
                        Debug.LogWarning($"Property or field '{part}' not found on {target.GetType().Name}");
                        return;
                    }
                    
                    if (target == null)
                    {
                        Debug.LogWarning($"Property or field '{part}' is null");
                        return;
                    }
                }
                
                string propertyName = propertyParts[propertyParts.Length - 1];
                
                // Get the field or property
                FieldInfo targetField = target.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo targetProperty = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                
                if (targetField != null)
                {
                    // Set the field value
                    object convertedValue = ConvertJTokenToType(value, targetField.FieldType);
                    targetField.SetValue(target, convertedValue);
                }
                else if (targetProperty != null && targetProperty.CanWrite)
                {
                    // Set the property value
                    object convertedValue = ConvertJTokenToType(value, targetProperty.PropertyType);
                    targetProperty.SetValue(target, convertedValue);
                }
                else
                {
                    Debug.LogWarning($"Property or field '{propertyName}' not found or not writable on {target.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting property '{propertyPath}' on {component.GetType().Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert a JToken to a specific type
        /// </summary>
        private static object ConvertJTokenToType(JToken token, Type targetType)
        {
            if (targetType == typeof(int))
            {
                return token.ToObject<int>();
            }
            else if (targetType == typeof(float))
            {
                return token.ToObject<float>();
            }
            else if (targetType == typeof(bool))
            {
                return token.ToObject<bool>();
            }
            else if (targetType == typeof(string))
            {
                return token.ToString();
            }
            else if (targetType == typeof(Vector2))
            {
                JArray array = token as JArray;
                if (array != null && array.Count >= 2)
                {
                    return new Vector2(array[0].ToObject<float>(), array[1].ToObject<float>());
                }
            }
            else if (targetType == typeof(Vector3))
            {
                JArray array = token as JArray;
                if (array != null && array.Count >= 3)
                {
                    return new Vector3(array[0].ToObject<float>(), array[1].ToObject<float>(), array[2].ToObject<float>());
                }
            }
            else if (targetType == typeof(Color))
            {
                JArray array = token as JArray;
                if (array != null && array.Count >= 3)
                {
                    if (array.Count >= 4)
                    {
                        return new Color(array[0].ToObject<float>(), array[1].ToObject<float>(), array[2].ToObject<float>(), array[3].ToObject<float>());
                    }
                    else
                    {
                        return new Color(array[0].ToObject<float>(), array[1].ToObject<float>(), array[2].ToObject<float>());
                    }
                }
            }
            
            // Default: try to convert directly
            return token.ToObject(targetType);
        }
    }
}
