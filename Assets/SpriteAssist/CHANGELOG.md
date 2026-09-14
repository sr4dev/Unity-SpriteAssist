# Changelog

## 1.5.9

- Unity Defaultの保存outlineをiShapeへ不変入力として渡し、接触する穴と島を保持する。
- 永続Meshはclamp前の座標と元画像サイズから生成する。
- 旧Meshの外観を編集可能なoutlineから復元するため、アフィンUV変換を追加する。
- 複製画像に残ったremapが別画像のPrefab所有権を変更しないよう保護する。
- importer versionを7に更新し、形状・outline・参照の回帰テストを追加する。
