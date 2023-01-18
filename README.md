# Unity-SpriteAssist
![image](README.gif)
![image](thickness.gif)

<p>
<img src="https://user-images.githubusercontent.com/9159336/213104399-26e02766-dd83-467c-a486-37b17b4a5081.png" width="250">
<img src="https://user-images.githubusercontent.com/9159336/213102784-29a08fcf-7a0f-46b3-a1b5-f3d2f3be3422.png" width="250">
<img src="https://user-images.githubusercontent.com/9159336/213102924-89149b98-49de-488b-9437-9a7bf9a2da48.png" width="250">
</p>

<p>
<img src="https://user-images.githubusercontent.com/9159336/213102642-ce282867-8a45-47d9-a426-dbc32a733483.png" width="250">
<img src="https://user-images.githubusercontent.com/9159336/213103066-fbfbf2fe-56b5-4f08-b0c3-f30d19b93fa1.png" width="250">
<img src="https://user-images.githubusercontent.com/9159336/213104230-d421c911-4441-4b3a-a2a2-928884cc753e.png" width="250">
</p>

## ℹ️About
Unity-SpriteAssist is an Unity extension that assist Sprite's mesh creation more conveniently.

## 🖥️System Requirements
Unity 2019.4 or later versions

## 📝Features
- **Dynamic preview**
- **Make a transparent, opaque, complex(transparent + opaque) mesh**
- **Convert Sprite to MeshRenderer Prefab**

## 📦Installation
### Unity Package Manager
You can add `https://github.com/sr4dev/Unity-SpriteAssist.git?path=Assets/SpriteAssist` to Package Manager.
![image](https://user-images.githubusercontent.com/9159336/99905771-42e89000-2d16-11eb-91a0-24ecf4af6afd.png)

### OpenUPM
https://openupm.com/packages/com.sr4dev.unity-spriteassist/
```
openupm add com.sr4dev.unity-spriteassist
```

### Manual installation
Clone or download this repository and copy `Assets/SpriteAssist` folder to your Unity project.


## ✏️How to use
- Select a Sprite asset in the Project window.
- Check the `Enable SpriteAssist` on the Inspector window.
- Adjust Mode and parameters.

### Mode
![image](https://user-images.githubusercontent.com/9159336/97450951-9ca7a580-1976-11eb-86f7-4e18775dd9b0.png)
- **Transparent Mesh**: Default Mode. Allow alpha pixel.
- **Opaque Mesh**: Disallow alpha pixel. You can use the mesh for opaque shader.
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

Even-odd(left), Non-zero(right)

![image](https://user-images.githubusercontent.com/9159336/97708967-f043fb80-1afc-11eb-954d-c6660cad6da6.png)
 
Wikipedia: [Non-zero winding](https://en.wikipedia.org/wiki/Nonzero-rule)

### Mesh Prefab
![image](https://user-images.githubusercontent.com/9159336/97451557-32433500-1977-11eb-8b57-32b6f15e04e6.png)
- **Prefab**: Linked Prefab with Sprite.
- **Create/Remove**: Create or remove a Prefab.
- **Default Transparent Shader**
- **Default Opaque Shader**

## ⚠️Warning!

### userData
This extension uses AssetImporter.userData of Texture asset. If your project already uses userData, it will be overridden.

Unity Document: [AssetImporter.userData](https://docs.unity3d.com/ScriptReference/AssetImporter-userData.html)

## ⚡Known Issues
In Unity 2022.2.x when using the **Sprite Atlas V2 - Enabled**, the following issues may occur:

- Preview is not displayed correctly.
- Mesh Prefab is not generated.
- sprites are displayed upside-down.
It is expected that resolving this issue may take some time, so it is recommended to use **Sprite Atlas V2 - Enabled For Builds** or **Sprite Atlas V1** for stable usage.

Unity Document: [Sprite Atlas v2](https://docs.unity3d.com/2022.2/Documentation/Manual/SpriteAtlasV2.html)

## 🗺️Roadmap
- Support pre-packed Sprite.
- Support Sprite Mode: Multiple.
- Add Sprite Animation example.
- Add tutorial(pdf, movies...).
- Release to the Asset Store.

## 🔣License
MIT License

## 📚Library
* Triangulation: [LibTessDotNet v1.1.13](https://github.com/speps/LibTessDotNet)
* Polygon Clipping: [Clipper v6.4.2](http://www.angusj.com/delphi/clipper.php)

## ☕Donation
<a href='https://ko-fi.com/sr4dev' target='_blank'><img height='35' style='border:0px;height:46px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com' />
