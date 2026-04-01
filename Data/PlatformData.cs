using System;

namespace BoplMapEditor.Data
{
    [Serializable]
    public class PlatformData
    {
        public float X;
        public float Y;
        public float HalfW;
        public float HalfH;
        public float Radius;
        public float Rotation;
        public int Type; // PlatformType: 0=grass,1=snow,2=ice,3=space,4=robot,5=slime

        public PlatformData() { }

        public PlatformData(float x, float y, float halfW, float halfH, float radius = 1f, float rotation = 0f, int type = 0)
        {
            X = x;
            Y = y;
            HalfW = halfW;
            HalfH = halfH;
            Radius = radius;
            Rotation = rotation;
            Type = type;
        }

        // Optional movement — null means static platform
        public PlatformMovement? Movement;

        public PlatformData Clone()
        {
            var c = new PlatformData(X, Y, HalfW, HalfH, Radius, Rotation, Type);
            c.Movement = Movement?.Clone();
            return c;
        }
    }
}
