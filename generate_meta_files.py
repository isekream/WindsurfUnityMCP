#!/usr/bin/env python3
"""
Generate .meta files for Unity package assets
This script creates basic meta files for all assets in the WindsurfUnityMCP directory
"""

import os
import uuid
import time

# Base directory for the Unity package
base_dir = "WindsurfUnityMCP"

# Meta file template for folders
folder_meta_template = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

# Meta file template for C# scripts
cs_meta_template = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

# Meta file template for assembly definition files
asmdef_meta_template = """fileFormatVersion: 2
guid: {guid}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

# Meta file template for text files (README, etc.)
text_meta_template = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

# Meta file template for package.json
package_meta_template = """fileFormatVersion: 2
guid: {guid}
PackageManifestImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

def generate_guid():
    """Generate a Unity-compatible GUID"""
    return str(uuid.uuid4()).replace("-", "")

def create_meta_file(path, is_folder=False):
    """Create a meta file for the given path"""
    meta_path = path + ".meta"
    
    # Skip if meta file already exists
    if os.path.exists(meta_path):
        print(f"Meta file already exists: {meta_path}")
        return
    
    guid = generate_guid()
    
    if is_folder:
        template = folder_meta_template
    elif path.endswith(".cs"):
        template = cs_meta_template
    elif path.endswith(".asmdef"):
        template = asmdef_meta_template
    elif path.endswith("package.json"):
        template = package_meta_template
    elif path.endswith(".md") or path.endswith(".txt"):
        template = text_meta_template
    else:
        template = text_meta_template  # Default to text importer
    
    with open(meta_path, "w") as f:
        f.write(template.format(guid=guid))
    
    print(f"Created meta file: {meta_path}")

def process_directory(directory):
    """Process a directory and create meta files for all assets"""
    # Create meta file for the directory itself
    create_meta_file(directory, is_folder=True)
    
    # Process all files and subdirectories
    for item in os.listdir(directory):
        path = os.path.join(directory, item)
        
        # Skip hidden files and meta files
        if item.startswith(".") or item.endswith(".meta"):
            continue
        
        if os.path.isdir(path):
            process_directory(path)
        else:
            create_meta_file(path)

if __name__ == "__main__":
    if os.path.exists(base_dir):
        print(f"Processing {base_dir}...")
        process_directory(base_dir)
        print("Done!")
    else:
        print(f"Error: {base_dir} directory not found")
