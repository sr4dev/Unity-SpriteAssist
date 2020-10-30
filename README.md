# Unity-SpriteAssist
![image](https://user-images.githubusercontent.com/9159336/97450592-40448600-1976-11eb-9cee-c4a47dc665b0.png)

## About
Unity-SpriteAssist is an Unity extension that assist Sprite's mesh creation more conviniently.

## System Requirements
Unity 2019.4 or later versions

## Features
- **Dynamic preview**
- **Make a transparent, opaque, complex(transparent + opaque) mesh**
- **Convert Sprite to MeshRenderer Prefab**

## Installation
Clone or download this repository and copy `Assets/SpriteAssist` folder to your Unity project.

(UnityPackage, Unity Package Manager is not supported yet)

## How to use
- Select a Sprite asset in the Project window.
- Check the `Enable SpriteAssist` on the Inspector window.
- Adjust Mode and parameters.

### Mode
![image](https://user-images.githubusercontent.com/9159336/97450951-9ca7a580-1976-11eb-86f7-4e18775dd9b0.png)
- **Transparent Mesh**: Default Mode. Allow alpha pixel.
- **Opaque Mesh**: Disallow alpha pixel. You can use the mesh for oapque shader.
- **Complex**: Separate area by alpha.
  - Alpha pixel is converted to Transparent Mesh area.
  - Non-alpha pixel is converted to Opaque Mesh area.
  - To Use Complex mode must be created Mesh Prefab.
  - Complex mode dose not override original Sprite mesh.
  
### Parameter
![image](https://user-images.githubusercontent.com/9159336/97451357-04f68700-1977-11eb-9445-77eac8a9efe3.png)
- **Detail**: Density and accuracy of the mesh to the Sprite outline.
- **Alpha Tolerance**: Threshold for transparency considered when generating the outline.
- **Detect Holes**: Detect holes from outline.
- **Edge Smoothing**: Smoothing sharp edges.
- **Non-zero Winding**: Use Non-zero winding rule. Default Winding rule is even-odd.
  - Even-odd(left), Non-zero(right)

![image](https://user-images.githubusercontent.com/9159336/97708967-f043fb80-1afc-11eb-954d-c6660cad6da6.png)
 
Wikipedia: [Non-zero winding](https://en.wikipedia.org/wiki/Nonzero-rule)

### Mesh Prefab
![image](https://user-images.githubusercontent.com/9159336/97451557-32433500-1977-11eb-8b57-32b6f15e04e6.png)
- **Prefab**: Linked Prefab width Sprite.
- **Create/Remove**: Create or remove a Prefab.
- **Default Transparent Shader**
- **Default Opaque Shader**

## Warning!

### userData
This extension uses AssetImporter.userData of Texture asset. If your project already uses userData, it will be overridden.

Unity Document: [AssetImporter.userData](https://docs.unity3d.com/ScriptReference/AssetImporter-userData.html)

## License
MIT License

## Libarary
* Triangulation: [LibTessDotNet v1.1.13](https://github.com/speps/LibTessDotNet)
* Polygon Clipping: [Clipper v6.4.2](http://www.angusj.com/delphi/clipper.php)

