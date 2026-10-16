# Changelog

## 1.5.10

- 画像とMesh Prefabを同時に複製したとき、生成されたコピー同士の参照を再接続する。
- 画像のみの複製では元Prefabへの重複リンクを解除し、既存Prefabの再importを複製として扱わない。
- 自動renameはルートMeshの参照元画像で判定し、import順による名前の往復を防ぐ。

## 1.5.9

- Unity Defaultの保存outlineをiShapeへ不変入力として渡し、接触する穴と島を保持する。
- 永続Meshはclamp前の座標と元画像サイズから生成する。
- 旧Meshの外観を編集可能なoutlineから復元するため、アフィンUV変換を追加する。
- 複製画像に残ったremapが別画像のPrefab所有権を変更しないよう保護する。
- importer versionを7に更新し、形状・outline・参照の回帰テストを追加する。
