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
        private const string TAG = "Untagged";

        [Flags]
        public enum Mode
        {
            UnityDefault = 0,
            TransparentMesh = 1 << 0,
            OpaqueMesh = 1 << 1,
            Complex = TransparentMesh | OpaqueMesh
        }
        
        public Mode mode = Mode.UnityDefault;

        public float transparentDetail = DETAIL;
        public byte transparentAlphaTolerance = ALPHA_TOLERANCE;
        public float opaqueDetail = DETAIL;
        public byte opaqueAlphaTolerance = ALPHA_TOLERANCE;
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

        public bool IsOverriden => mode != Mode.UnityDefault;
        
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

            return data;
        }
    }
}
