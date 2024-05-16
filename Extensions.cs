using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Guneline
{
    public static class Extensions
    {
        public static Vector2 HalfDimensions(this MTexture texture)
            => new Vector2(texture.Width / 2, texture.Height / 2);

        public static float ToRotation(this Vector2 vector) => (float)Math.Atan2(vector.Y, vector.X);
    }
}
