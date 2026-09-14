using System;
using UnityEngine;

namespace SpriteAssist
{
    [Serializable]
    public class SpriteConfigData
    {
        private const float DETAIL = 0.3f;
        private const byte ALPHA_TOLERANCE = 10;
        private const float EDGE_SMOOTHING = 1f;
        private const bool DETECT_HOLES = true;
        private const float PIXELS_PER_UNIT = 100;
        private const float OPAQUE_EXTRUDE = 0.064f;
        private const string TAG = "Untagged";

        [Flags]
        public enum Mode
        {
            UnityDefaultForTransparent = 0,
            UnityDefaultForOpaque = 1 << 2,
            TransparentMesh = 1 << 0,
            OpaqueMesh = 1 << 1,
            ComplexMesh = TransparentMesh | OpaqueMesh,
            GridMesh = 1 << 3,
            OpaqueEdgeGridMesh = OpaqueMesh | GridMesh,
            PixelMesh = 1 << 4,
        }

        public static bool IsUnityDefaultMode(Mode mode)
        {
            return mode == Mode.UnityDefaultForTransparent || mode == Mode.UnityDefaultForOpaque;
        }
        
        public Mode mode = Mode.UnityDefaultForTransparent;

        public float transparentDetail = DETAIL;
        public byte transparentAlphaTolerance = ALPHA_TOLERANCE;
        public float opaqueDetail = DETAIL;
        public byte opaqueAlphaTolerance = ALPHA_TOLERANCE;
        public float opaqueExtrude = OPAQUE_EXTRUDE;
        public int gridSize;
        public float gridTolerance;
        public bool detectHoles = DETECT_HOLES;
        public float edgeSmoothing = EDGE_SMOOTHING;
        public bool useNonZero = false;

        public string transparentShaderName;
        public string opaqueShaderName;
        public float thickness;

        public bool overrideTag;
        public bool overrideLayer;
        public bool overrideSortingLayer;

        public string tag = TAG;
        public int layer;
        public int sortingLayerId;
        public int sortingOrder;
        public bool isCorrectNormal;
        public bool isWeldVertices;

        // 旧 Mesh から検証して復元した UV 変換。輪郭編集や PPU/pivot 変更にも追従する正規化画像座標を入力とする。
        public MeshUvTransform meshUv;

        public static SpriteConfigData GetData(string jsonData)
        {
            SpriteConfigData data = null;

            try
            {
                data = JsonUtility.FromJson<SpriteConfigData>(jsonData);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            if (data == null)
            {
                data = new SpriteConfigData();
            }

            // JsonUtility は省略された参照型も構築する。空/恒等変換は未指定と同じ扱いにする。
            if (data.meshUv != null && (data.meshUv.version == 0 ||
                data.meshUv.version == 1 && data.meshUv.u.Equals(Vector3.right) && data.meshUv.v.Equals(Vector3.up))) data.meshUv = null;

            return data;
        }
    }
}
