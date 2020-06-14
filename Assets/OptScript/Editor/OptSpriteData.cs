using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OptSprite
{
    [Serializable]
    public class OptSpriteData
    {

        private const float DETAIL = 0.3f;
        private const byte ALPHA_TOLERANCE = 10;
        private const byte VERTEX_MERGE_DISTANCE = 3;
        private const bool DETECT_HOLES = true;

        public bool overriden;

        [Range(0, 10)]
        public float detail = DETAIL;

        [Range(0, 254)]
        public byte alphaTolerance = ALPHA_TOLERANCE;

        [Range(0, 30)]
        public byte vertexMergeDistance = VERTEX_MERGE_DISTANCE;

        public bool detectHoles = DETECT_HOLES;

        public static OptSpriteData GetData(string jsonData)
        {
            OptSpriteData data = null;

            try
            {
                data = JsonUtility.FromJson<OptSpriteData>(jsonData);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            if (data == null)
            {
                data = new OptSpriteData();
            }

            return data;
        }
    }
}
