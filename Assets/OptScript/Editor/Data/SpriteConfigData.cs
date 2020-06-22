using System;
using UnityEngine;

namespace OptSprite
{
    [Serializable]
    public class SpriteConfigData
    {
        private const float DETAIL = 0.3f;
        private const byte ALPHA_TOLERANCE = 10;
        private const byte VERTEX_MERGE_DISTANCE = 3;
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

        public bool hasMeshPrefab;

        [Range(0, 10)]
        public float detail = DETAIL;

        [Range(0, 254)]
        public byte alphaTolerance = ALPHA_TOLERANCE;

        [Range(0, 30)]
        public byte vertexMergeDistance = VERTEX_MERGE_DISTANCE;

        [Range(0, 254)]
        public byte opaqueAlphaTolerance = ALPHA_TOLERANCE;


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
