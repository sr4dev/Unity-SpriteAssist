using LibTessDotNet;
using System;
using UnityEngine;

namespace OptSprite
{
    [Serializable]
    public class SpriteConfigData
    {
        private const float DETAIL = 0.3f;
        private const byte ALPHA_TOLERANCE = 10;
        private const float EDGE_SMOOTHING = 1f;
        private const bool DETECT_HOLES = true;

        [Flags]
        public enum Mode
        {
            TransparentMesh = 1 << 0,
            OpaqueMesh = 1 << 1,
            Complex = TransparentMesh | OpaqueMesh
        }

        public bool overriden;

        public Mode mode = Mode.TransparentMesh;
        public WindingRule windingRule = WindingRule.EvenOdd;

        [Range(0, 10)]
        public float transparentDetail = DETAIL;

        [Range(1, 255)]
        public byte transparentAlphaTolerance = ALPHA_TOLERANCE;

        [Range(0, 10)]
        public float opaqueDetail = DETAIL;

        [Range(0, 255)]
        public byte opaqueAlphaTolerance = ALPHA_TOLERANCE;
        
        [Range(0, 1)]
        public float edgeSmoothing = EDGE_SMOOTHING;

        public bool detectHoles = DETECT_HOLES;

        public string meshPrefabGuid;

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
