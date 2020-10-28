# Unity-SpriteAssist

## About
Unity-SpriteAssist는 Unity 프로젝트의 Sprite 메쉬 생성에 관한 몇가지 편리한 확장 기능을 제공합니다.

이 확장 기능은 Unity 2019.4에서 개발되었습니다.

## Features
- **Preview**: 현재 선택된 Sprite가 어떤 형태의 메쉬를 지니는지 확인할 수 있습니다.
- **Mode**: Sprite의 메쉬 생성 방식을 다양하게 조절할 수 있습니다.
- **Mesh Prefab**: Sprite를 Prefab화 시킴으로서 SpriteRenderer가 아닌 MeshRenderer를 사용할 수 있게 해줍니다.

## Installation
이 레포지토리를 클론 혹은 다운로드 하여 `Assets/SpriteAssist` 폴더를 자신의 프로젝트에 복사합니다.

(UnityPackage 파일 혹은 PackageManager를 통한 설치는 아지 지원하지 않습니다)

## How to use
- Sprite를 선택합니다.
- Inspector창에서 `Enable SpriteAssist`를 눌러 활성화합니다.
- 어떤 Mode로 메쉬를 조절할 것인지 선택합니다.
- Apply 버튼을 눌러 적용합니다.

### Mode
- **Transparent Mesh**: 기본값. 일반적인 메쉬 생성.
- **Opaque Mesh**: 불투명한 영역만으로 이루어진 메쉬를 생성.
- **Complex**: 투명 영역은 Transparent Mesh로, 불투명 영역은 Opaque Mesh로 생성.
  - 이 모드는 Mesh Prefab을 생성해야만 사용할 수 있습니다.
  - 이 모드는 Sprite의 메쉬에는 영향을 주지 않습니다.
  
### Parameter
- **Detail**: 값이 높을수록 더 정밀하고 촘촘하게 정점을 배치합니다.
- **Alpha Tolerance**: 값이 높을수록 투명 영역이 작아집니다. 
- **Detect Holes**: 구멍이 난 형태의 메쉬를 허용합니다.
- **Edge Smoothing**: 값이 높을수록 많은 수의 정점을 사용하여 곡선을 부드럽게 처리합니다.

### Mesh Prefab
- **Prefab**: Sprite와 연결된 Prefab입니다. 파라미터가 수정될 경우 연결된 Prefab에도 자동으로 반영됩니다.
- **Create/Remove**: Prefab을 생성하여 Sprite와 연결합니다 / 연결을 끊고 Prefab을 삭제합니다.
- **Default Transparent Shader**: Prefab 생성시 사용할 투명 영역에 대한 Shader입니다.
- **Default Opaque Shader**: Prefab 생성시 사용할 불투명 영역에 대한 Shader입니다.

## Warning!

### userData
이 확장 기능은 Texture 어셋의 **userData** 영역을 사용합니다. 만약 현재 작업중인 프로젝트가 **userData** 영역을 사용하고 있다면, 이 확장 기능을 도입하게 될 경우 기존 **userData**에 '손상'을 줄 수 있습니다. **userData**에 대해서 궁금한 내용이 있다면 아래의 문서를 참조하시기 바랍니다.

Unity Document: [AssetImporter.userData](https://docs.unity3d.com/ScriptReference/AssetImporter-userData.html)

## License
MIT

## Links
* Triangulation: [LibTessDotNet v1.1.13](https://github.com/speps/LibTessDotNet)
* Polygon Clipping: [Clipper v6.4.2](http://www.angusj.com/delphi/clipper.php)
