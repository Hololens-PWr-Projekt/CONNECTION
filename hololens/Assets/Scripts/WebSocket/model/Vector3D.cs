using System;

namespace Model.Vector3D
{

    [Serializable]
    public class Vector3D
    {
        private float x;
        private float y;
        private float z;

        public Vector3D(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }
        public float Z { get => z; set => z = value; }
    }
}