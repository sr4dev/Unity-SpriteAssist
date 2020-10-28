# Unity-SpriteAssist

## About
Unity-SpriteAssist는 Unity 프로젝트의 Sprite 메쉬 생성에 관한 몇가지 편리한 확장 기능을 제공합니다.

이 확장 기능은 Unity 2019.4에서 개발되었습니다.

## Features
* 현재 선택된 Sprite가 어떤 형태의 메쉬를 지니는지 확인할 수 있습니다.
* Sprite의 메쉬 생성 방식을 조절할 수 있습니다.
* Sprite를 Prefab화 시킴으로서 SpriteRenderer가 아닌 MeshRenderer를 사용할 수 있게 해줍니다.

## Installation
이 레포지토리를 클론 혹은 다운로드 하여 `Assets/SpriteAssist` 폴더를 자신의 프로젝트에 복사합니다.

(UnityPackage 파일 혹은 PackageManager를 통한 설치는 아지 지원하지 않습니다)

## How to use


## Warning!

### userData
이 확장 기능은 Texture 어셋의 userData 영역을 사용합니다.
만약 현재 작업중인 프로젝트가 userData 영역을 사용하고 있다면, 이 확장 기능을 도입하게 될 경우 기존 userData에 손상을 줄 수 있습니다.
userData에 대해서 궁금한 내용이 있다면 아래의 문서를 참조하시기 바랍니다.

For more info see:
https://docs.unity3d.com/ScriptReference/AssetImporter-userData.html

## License
MIT

## Links
* Triangulation: [LibTessDotNet v1.1.13](https://github.com/speps/LibTessDotNet)
* Polygon Clipping: [Clipper v6.4.2](http://www.angusj.com/delphi/clipper.php)
