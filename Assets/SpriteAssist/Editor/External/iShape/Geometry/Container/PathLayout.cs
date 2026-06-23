using UnityEngine;

namespace iShape.Geometry.Container {

    [System.Serializable]
    public struct PathLayout {
        
        public int begin;
        public int length;
        [SerializeField]
        private byte clockWise;
        public bool isClockWise => this.clockWise == 1;

        public int end => begin + length - 1;

        public PathLayout(int begin, int length, bool isClockWise) {
            this.begin = begin;
            this.length = length;
            this.clockWise =  isClockWise ? (byte)1 : (byte)0;
        }
    }

}