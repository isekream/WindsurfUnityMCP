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
    /// Asset management functions for MCP
    /// </summary>
    public static partial class McpFunctions
    {
        /// <summary>
        /// Perform asset operations in Unity
        /// </summary>
        public static async Task<JObject> ManageAssetAsync(JObject parameters)
        {
            string action = parameters["action"]?.ToString();
            string path = parameters["path"]?.ToString();
            string assetType = parameters["asset_type"]?.ToString();
            JObject properties = parameters["properties"] as JObject;
            string destination = parameters["destination"]?.ToString();
            string searchPattern = parameters["search_pattern"]?.ToString();
            string filterType = parameters["filter_type"]?.ToString();
            string filterDateAfter = parameters["filter_date_after"]?.ToString();
            int? pageNumber = parameters["page_number"]?.ToObject<int>();
            int? pageSize = parameters["page_size"]?.ToObject<int>();
            bool generatePreview = parameters["generate_preview"]?.ToObject<bool>() ?? false;
            
            if (string.IsNullOrEmpty(action))
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = "Action is required"
                };
            }
            
            if (string.IsNullOrEmpty(path) && action != "search")
            {
                return new JObject
                {
                    ["success"] = false,
                    ["error"] = "Path is required"
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
                        case "import":
                            // Check if the file exists
                            if (!File.Exists(path))
                            {
                                success = false;
                                message = $"File not found: {path}";
                                return;
                            }
                            
                            // Import the asset
                            AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
                            
                            success = true;
                            message = $"Asset '{path}' imported successfully";
                            break;
                            
                        case "create":
                            if (string.IsNullOrEmpty(assetType))
                            {
                                success = false;
                                message = "Asset type is required for create action";
                                return;
                            }
                            
                            // Create the directory if it doesn't exist
                            string directory = Path.GetDirectoryName(path);
                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }
                            
                            switch (assetType.ToLower())
                            {
                                case "material":
                                    // Create a new material
                                    Material material = new Material(Shader.Find("Standard"));
                                    
                                    // Set properties if provided
                                    if (properties != null)
                                    {
                                        SetMaterialProperties(material, properties);
                                    }
                                    
                                    // Save the material
                                    AssetDatabase.CreateAsset(material, path);
                                    
                                    success = true;
                                    message = $"Material '{path}' created successfully";
                                    break;
                                    
                                case "folder":
                                    // Create a folder
                                    if (AssetDatabase.IsValidFolder(path))
                                    {
                                        success = false;
                                        message = $"Folder '{path}' already exists";
                                    }
                                    else
                                    {
                                        string parentFolder = Path.GetDirectoryName(path).Replace('\\', '/');
                                        string folderName = Path.GetFileName(path);
                                        
                                        AssetDatabase.CreateFolder(parentFolder, folderName);
                                        
                                        success = true;
                                        message = $"Folder '{path}' created successfully";
                                    }
                                    break;
                                    
                                case "texture":
                                    // Create a new texture
                                    Texture2D texture = new Texture2D(1024, 1024);
                                    
                                    // Set properties if provided
                                    if (properties != null)
                                    {
                                        SetTextureProperties(texture, properties);
                                    }
                                    
                                    // Save the texture
                                    byte[] pngData = texture.EncodeToPNG();
                                    File.WriteAllBytes(path, pngData);
                                    AssetDatabase.ImportAsset(path);
                                    
                                    success = true;
                                    message = $"Texture '{path}' created successfully";
                                    break;
                                    
                                default:
                                    success = false;
                                    message = $"Unsupported asset type: {assetType}";
                                    break;
                            }
                            
                            // Refresh the asset database
                            AssetDatabase.Refresh();
                            break;
                            
                        case "modify":
                            // Check if the asset exists
                            if (!File.Exists(path))
                            {
                                success = false;
                                message = $"Asset not found: {path}";
                                return;
                            }
                            
                            // Get the asset type
                            string extension = Path.GetExtension(path).ToLower();
                            
                            switch (extension)
                            {
                                case ".mat":
                                    // Modify a material
                                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                                    if (mat != null && properties != null)
                                    {
                                        SetMaterialProperties(mat, properties);
                                        EditorUtility.SetDirty(mat);
                                        AssetDatabase.SaveAssets();
                                        
                                        success = true;
                                        message = $"Material '{path}' modified successfully";
                                    }
                                    else
                                    {
                                        success = false;
                                        message = $"Failed to load material: {path}";
                                    }
                                    break;
                                    
                                case ".png":
                                case ".jpg":
                                case ".jpeg":
                                    // Modify a texture
                                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                                    if (importer != null && properties != null)
                                    {
                                        SetTextureImporterProperties(importer, properties);
                                        importer.SaveAndReimport();
                                        
                                        success = true;
                                        message = $"Texture '{path}' modified successfully";
                                    }
                                    else
                                    {
                                        success = false;
                                        message = $"Failed to load texture importer: {path}";
                                    }
                                    break;
                                    
                                default:
                                    success = false;
                                    message = $"Unsupported asset type for modification: {extension}";
                                    break;
                            }
                            break;
                            
                        case "delete":
                            // Check if the asset exists
                            if (File.Exists(path) || AssetDatabase.IsValidFolder(path))
                            {
                                AssetDatabase.DeleteAsset(path);
                                
                                success = true;
                                message = $"Asset '{path}' deleted successfully";
                            }
                            else
                            {
                                success = false;
                                message = $"Asset not found: {path}";
                            }
                            break;
                            
                        case "duplicate":
                            if (string.IsNullOrEmpty(destination))
                            {
                                success = false;
                                message = "Destination is required for duplicate action";
                                return;
                            }
                            
                            // Check if the source asset exists
                            if (File.Exists(path) || AssetDatabase.IsValidFolder(path))
                            {
                                // Create the destination directory if it doesn't exist
                                string destDir = Path.GetDirectoryName(destination);
                                if (!Directory.Exists(destDir))
                                {
                                    Directory.CreateDirectory(destDir);
                                }
                                
                                // Duplicate the asset
                                string result = AssetDatabase.CopyAsset(path, destination) ? "success" : "failed";
                                
                                if (result == "success")
                                {
                                    success = true;
                                    message = $"Asset '{path}' duplicated to '{destination}' successfully";
                                }
                                else
                                {
                                    success = false;
                                    message = $"Failed to duplicate asset '{path}' to '{destination}'";
                                }
                            }
                            else
                            {
                                success = false;
                                message = $"Source asset not found: {path}";
                            }
                            break;
                            
                        case "move":
                        case "rename":
                            if (string.IsNullOrEmpty(destination))
                            {
                                success = false;
                                message = "Destination is required for move/rename action";
                                return;
                            }
                            
                            // Check if the source asset exists
                            if (File.Exists(path) || AssetDatabase.IsValidFolder(path))
                            {
                                // Create the destination directory if it doesn't exist
                                string destDir = Path.GetDirectoryName(destination);
                                if (!Directory.Exists(destDir))
                                {
                                    Directory.CreateDirectory(destDir);
                                }
                                
                                // Move/rename the asset
                                string result = AssetDatabase.MoveAsset(path, destination);
                                
                                if (string.IsNullOrEmpty(result))
                                {
                                    success = true;
                                    message = $"Asset '{path}' moved/renamed to '{destination}' successfully";
                                }
                                else
                                {
                                    success = false;
                                    message = $"Failed to move/rename asset: {result}";
                                }
                            }
                            else
                            {
                                success = false;
                                message = $"Source asset not found: {path}";
                            }
                            break;
                            
                        case "search":
                            // Search for assets
                            string[] guids;
                            
                            if (!string.IsNullOrEmpty(searchPattern))
                            {
                                // Search by pattern
                                guids = AssetDatabase.FindAssets(searchPattern, new[] { path ?? "Assets" });
                            }
                            else
                            {
                                // Get all assets in the path
                                guids = AssetDatabase.FindAssets("", new[] { path ?? "Assets" });
                            }
                            
                            List<string> assetPaths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();
                            
                            // Filter by type
                            if (!string.IsNullOrEmpty(filterType))
                            {
                                assetPaths = assetPaths.Where(p => Path.GetExtension(p).ToLower() == $".{filterType.ToLower()}").ToList();
                            }
                            
                            // Filter by date
                            if (!string.IsNullOrEmpty(filterDateAfter))
                            {
                                if (DateTime.TryParse(filterDateAfter, out DateTime dateAfter))
                                {
                                    assetPaths = assetPaths.Where(p => File.GetLastWriteTime(p) > dateAfter).ToList();
                                }
                            }
                            
                            // Apply pagination
                            int total = assetPaths.Count;
                            int page = pageNumber ?? 1;
                            int size = pageSize ?? 20;
                            int skip = (page - 1) * size;
                            
                            assetPaths = assetPaths.Skip(skip).Take(size).ToList();
                            
                            // Create the result
                            JArray assets = new JArray();
                            foreach (string assetPath in assetPaths)
                            {
                                JObject asset = new JObject
                                {
                                    ["path"] = assetPath,
                                    ["name"] = Path.GetFileName(assetPath),
                                    ["type"] = Path.GetExtension(assetPath).TrimStart('.'),
                                    ["lastModified"] = File.GetLastWriteTime(assetPath).ToString("o")
                                };
                                
                                // Generate preview if requested
                                if (generatePreview)
                                {
                                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                                    if (obj != null)
                                    {
                                        Texture2D preview = AssetPreview.GetAssetPreview(obj);
                                        if (preview != null)
                                        {
                                            asset["preview"] = Convert.ToBase64String(preview.EncodeToPNG());
                                        }
                                    }
                                }
                                
                                assets.Add(asset);
                            }
                            
                            data = new JObject
                            {
                                ["total"] = total,
                                ["page"] = page,
                                ["size"] = size,
                                ["assets"] = assets
                            };
                            
                            success = true;
                            message = $"Found {total} assets, showing {assets.Count}";
                            break;
                            
                        case "get_info":
                            // Check if the asset exists
                            if (File.Exists(path) || AssetDatabase.IsValidFolder(path))
                            {
                                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                                
                                if (asset != null)
                                {
                                    data = new JObject
                                    {
                                        ["path"] = path,
                                        ["name"] = Path.GetFileName(path),
                                        ["type"] = asset.GetType().Name,
                                        ["lastModified"] = File.GetLastWriteTime(path).ToString("o"),
                                        ["size"] = new FileInfo(path).Length
                                    };
                                    
                                    // Get asset-specific information
                                    if (asset is Material material)
                                    {
                                        data["shader"] = material.shader.name;
                                        data["renderQueue"] = material.renderQueue;
                                    }
                                    else if (asset is Texture2D texture)
                                    {
                                        data["width"] = texture.width;
                                        data["height"] = texture.height;
                                        data["format"] = texture.format.ToString();
                                    }
                                    
                                    success = true;
                                    message = $"Asset info retrieved for '{path}'";
                                }
                                else
                                {
                                    success = false;
                                    message = $"Failed to load asset: {path}";
                                }
                            }
                            else
                            {
                                success = false;
                                message = $"Asset not found: {path}";
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
        /// Set properties on a material
        /// </summary>
        private static void SetMaterialProperties(Material material, JObject properties)
        {
            foreach (KeyValuePair<string, JToken> property in properties)
            {
                string propertyName = property.Key;
                JToken value = property.Value;
                
                switch (propertyName.ToLower())
                {
                    case "color":
                    case "maincolor":
                        if (value is JArray colorArray && colorArray.Count >= 3)
                        {
                            Color color = new Color(
                                colorArray[0].ToObject<float>(),
                                colorArray[1].ToObject<float>(),
                                colorArray[2].ToObject<float>(),
                                colorArray.Count >= 4 ? colorArray[3].ToObject<float>() : 1.0f
                            );
                            
                            material.color = color;
                        }
                        break;
                        
                    case "shader":
                        string shaderName = value.ToString();
                        Shader shader = Shader.Find(shaderName);
                        if (shader != null)
                        {
                            material.shader = shader;
                        }
                        break;
                        
                    case "maintexture":
                        string texturePath = value.ToString();
                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                        if (texture != null)
                        {
                            material.mainTexture = texture;
                        }
                        break;
                        
                    case "renderqueue":
                        material.renderQueue = value.ToObject<int>();
                        break;
                        
                    default:
                        // Try to set the property directly
                        if (material.HasProperty(propertyName))
                        {
                            if (value is JArray array)
                            {
                                if (array.Count == 4)
                                {
                                    // Assume it's a color
                                    Color color = new Color(
                                        array[0].ToObject<float>(),
                                        array[1].ToObject<float>(),
                                        array[2].ToObject<float>(),
                                        array[3].ToObject<float>()
                                    );
                                    
                                    material.SetColor(propertyName, color);
                                }
                                else if (array.Count == 3)
                                {
                                    // Assume it's a vector3
                                    Vector3 vector = new Vector3(
                                        array[0].ToObject<float>(),
                                        array[1].ToObject<float>(),
                                        array[2].ToObject<float>()
                                    );
                                    
                                    material.SetVector(propertyName, vector);
                                }
                                else if (array.Count == 2)
                                {
                                    // Assume it's a vector2
                                    Vector2 vector = new Vector2(
                                        array[0].ToObject<float>(),
                                        array[1].ToObject<float>()
                                    );
                                    
                                    material.SetVector(propertyName, vector);
                                }
                            }
                            else if (value.Type == JTokenType.Float || value.Type == JTokenType.Integer)
                            {
                                material.SetFloat(propertyName, value.ToObject<float>());
                            }
                            else if (value.Type == JTokenType.Boolean)
                            {
                                material.SetInt(propertyName, value.ToObject<bool>() ? 1 : 0);
                            }
                            else if (value.Type == JTokenType.String)
                            {
                                // Try to load a texture
                                string texPath = value.ToString();
                                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                                if (tex != null)
                                {
                                    material.SetTexture(propertyName, tex);
                                }
                            }
                        }
                        break;
                }
            }
        }
        
        /// <summary>
        /// Set properties on a texture
        /// </summary>
        private static void SetTextureProperties(Texture2D texture, JObject properties)
        {
            foreach (KeyValuePair<string, JToken> property in properties)
            {
                string propertyName = property.Key;
                JToken value = property.Value;
                
                switch (propertyName.ToLower())
                {
                    case "filtermode":
                        if (Enum.TryParse<FilterMode>(value.ToString(), true, out FilterMode filterMode))
                        {
                            texture.filterMode = filterMode;
                        }
                        break;
                        
                    case "wrapmode":
                        if (Enum.TryParse<TextureWrapMode>(value.ToString(), true, out TextureWrapMode wrapMode))
                        {
                            texture.wrapMode = wrapMode;
                        }
                        break;
                        
                    case "anisolevel":
                        texture.anisoLevel = value.ToObject<int>();
                        break;
                }
            }
        }
        
        /// <summary>
        /// Set properties on a texture importer
        /// </summary>
        private static void SetTextureImporterProperties(TextureImporter importer, JObject properties)
        {
            bool needsReimport = false;
            
            foreach (KeyValuePair<string, JToken> property in properties)
            {
                string propertyName = property.Key;
                JToken value = property.Value;
                
                switch (propertyName.ToLower())
                {
                    case "isreadable":
                        importer.isReadable = value.ToObject<bool>();
                        needsReimport = true;
                        break;
                        
                    case "mipmapsenabled":
                        importer.mipmapEnabled = value.ToObject<bool>();
                        needsReimport = true;
                        break;
                        
                    case "npotscale":
                        if (Enum.TryParse<TextureImporterNPOTScale>(value.ToString(), true, out TextureImporterNPOTScale npotScale))
                        {
                            importer.npotScale = npotScale;
                            needsReimport = true;
                        }
                        break;
                        
                    case "filtermode":
                        if (Enum.TryParse<FilterMode>(value.ToString(), true, out FilterMode filterMode))
                        {
                            importer.filterMode = filterMode;
                            needsReimport = true;
                        }
                        break;
                        
                    case "texturecompression":
                        if (Enum.TryParse<TextureImporterCompression>(value.ToString(), true, out TextureImporterCompression compression))
                        {
                            importer.textureCompression = compression;
                            needsReimport = true;
                        }
                        break;
                        
                    case "maxsize":
                        importer.maxTextureSize = value.ToObject<int>();
                        needsReimport = true;
                        break;
                }
            }
            
            if (needsReimport)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
