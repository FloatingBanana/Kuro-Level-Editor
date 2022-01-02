using System;
using System.Numerics;

namespace Kuro.Renderer
{
    public class TCamera
    {
        public Vector3 Position {get; set;}
        public Vector3 Front {get; set;}
        public Vector3 Up {get; set;}

        public float AspectRatio {get; set;}
        
        public float Yaw {get; set;} = -90f;
        public float Pitch {get; set;}

        private float zoom = 45f;

        public TCamera(Vector3 position, Vector3 front, Vector3 up, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
            Front = front;
            Up = up;
        }

        public void ModifyZoom(float amount)
        {
            zoom = Math.Clamp(zoom - amount, 1.0f, 45f);
        }

        public void ModifyDirection(float xOffset, float yOffset)
        {
            Yaw += xOffset;
            Pitch -= yOffset;

            Pitch = Math.Clamp(Pitch, -89f, 89f);

            var camDir = Vector3.Zero;
            var oneRad = MathF.PI / 180f;

            camDir.X = MathF.Cos(Yaw * oneRad) * MathF.Cos(Pitch * oneRad);
            camDir.Y = MathF.Sin(Pitch * oneRad);
            camDir.Z = MathF.Sin(Yaw * oneRad) * MathF.Cos(Pitch * oneRad);

            Front = Vector3.Normalize(camDir);
        }

        public Matrix4x4 ViewMatrix => Matrix4x4.CreateLookAt(Position, Position + Front, Up);

        public Matrix4x4 ProjectionMatrix => Matrix4x4.CreatePerspectiveFieldOfView((MathF.PI / 180f) * zoom, AspectRatio, 0.1f, 1000.0f);
    }
}