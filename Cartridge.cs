using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Guneline
{
    public class Cartridge : Entity
    {
        private Vector2 velocity;

        private float rotation;

        private int expireTimer;

        private const float gravAccel = 0.2f;
        private const float rotationIncrement = 10;

        public Cartridge(Vector2 position, Vector2 velocity, Actor owner)
        {
            Position = position;

            this.velocity = velocity;

            rotation = Calc.Random.NextFloat(MathHelper.TwoPi);

            expireTimer = 180;

            if (Bullet.ValidTracker(owner))
            {
                (owner.Scene as Level).Add(this);
            }
        }

        public override void Update()
        {
            Position += velocity;

            if (velocity.Y < 9)
            {
                velocity.Y += gravAccel;
            }

            rotation += MathHelper.ToRadians(rotationIncrement * Math.Sign(velocity.X));

            if (--expireTimer <= 0)
            {
                RemoveSelf();
            }
        }

        public override void Render()
        {
            Guneline.cartridgeTex.Draw(Position, Guneline.cartridgeTex.HalfDimensions(), Color.White, 1, rotation);
        }
    }
}
