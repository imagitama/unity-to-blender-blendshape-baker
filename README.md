# Unity To Blender Blendshape Baker

A Unity plugin that exports the current value of your mesh blendshapes, detects if they can be baked (ie not animated by any of your controllers) and exports it so it can be imported into Blender and each shape key will be added `_bake` at the end so they can be baked by CATS.

## Why is this useful

Baking "always on" blendshapes is an easy FPS gain in Unity games like VRChat. You can do it in Unity with a plugin and you can manually do it in Blender but this tool helps you do it in Blender.

## Usage

**It is strongly recommended that you backup your Unity and Blender projects!**

1. Install the Unity package into your project and go to PeanutTools in the menu and open the panel
2. Select a transform (such as a VRChat avatar)
3. Click "Run" then click "Export" to output it for Blender
4. Install the Blender plugin into your project and open the "Misc" panel
5. Select your armature
6. Click "Import" and find the text file
7. Click "Apply" to rename your shape keys (if baking)

## Ideas

- perform the bake ourselves in Blender (without CATS)
- perform bake in Unity
