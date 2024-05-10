using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using System.Collections.Generic;

namespace Guneline
{
    [Tracked]
    public class Bullet : Entity
    {
        public Rectangle Hitbox => new Rectangle((int)Position.X - 2, (int)Position.Y - 2, 4, 4);

        private Vector2 velocity;

        private readonly Actor owner;

        private int lifetime;

        private bool dead;

        private int updateCount;

        private const int extraUpdates = 24;

        private readonly List<Bumper> alreadyBouncedOffOf;

        public Bullet(Vector2 position, Vector2 velocity, Actor owner)
        {
            Position = position;

            this.velocity = velocity;

            this.owner = owner;

            lifetime = 600;

            alreadyBouncedOffOf = new List<Bumper>();

            if (ValidTracker(owner))
            {
                (owner.Scene as Level).Add(this);
            }
        }

        public override void Update()
        {
            if (updateCount > extraUpdates)
            {
                updateCount = 0;

                return;
            }

            Position += velocity;

            if (ValidTracker(owner))
            {
                CollisionCheck();

                if (Calc.Random.Next(6) == 0)
                {
                    (owner.Scene as Level).Particles.Emit(ParticleTypes.Dust, Position, Color.White);
                }
            }

            if (--lifetime <= 0)
            {
                Kill();
            }

            updateCount++;

            Update();
        }

        public override void Render()
        {
            if (ValidTracker(owner))
            {
                (owner.Scene as Level).Particles.Emit(ParticleTypes.Dust, Position, Color.White);
            }
        }

        private void CollisionCheck()
        {
            if (owner.Collider.Bounds.Intersects(Hitbox) && alreadyBouncedOffOf.Count > 0)
            {
                if (owner is Player p)
                {
                    p.Die(velocity, true);
                }

                Kill();

                return;
            }

            if (owner.Scene.CollideFirst<Player>(Hitbox) is Player player && player != owner && !dead && !SaveData.Instance.Assists.Invincible)
            {
                player.Die(velocity, true);

                Kill();

                return;
            }

            if (owner.Scene.CollideFirst<Seeker>(Hitbox) is Seeker seeker && !dead)
            {
                if (BootlegStunSeeker(seeker))
                {
                    Kill();

                    return;
                }
            }

            if (owner.Scene.CollideFirst<AngryOshiro>(Hitbox) is AngryOshiro angryOshiro && !dead)
            {
                BootlegOshiroBounce(angryOshiro);

                Kill();

                return;
            }

            if (owner.Scene.CollideFirst<TheoCrystal>(Hitbox) is TheoCrystal theo && !dead)
            {
                theo.Die();

                Kill();

                return;
            }

            if (owner.Scene.CollideFirst<DashBlock>(Hitbox) is DashBlock dBlock && !dead)
            {
                dBlock.Break(Position, velocity, true, true);

                Kill();

                return;
            }

            if (owner.Scene.CollideFirst<CrystalStaticSpinner>(Hitbox) is CrystalStaticSpinner spinner && !dead)
            {
                spinner.Destroy();

                Kill();

                return;
            }

            if (owner.Scene.CollideFirst<HeartGem>(Hitbox) is HeartGem heartGem && !dead)
            {
                if (owner is Player p)
                {
                    Guneline.heartGemCollect.Invoke(heartGem, new object[] { p });
                }

                Kill();

                return;
            }

            if (owner.Scene.CollideFirst<FinalBoss>(Hitbox) is FinalBoss boss && !dead)
            {
                if (!boss.Sitting && owner is Player p)
                {
                    boss.OnPlayer(p);
                }

                Kill();

                return;
            }

            if (owner.Scene.CollideFirst<DustStaticSpinner>(Hitbox) is DustStaticSpinner dSSpinner && !dead)
            {
                dSSpinner.RemoveSelf();

                Kill();

                return;
            }

            foreach (Entity entity in (owner.Scene as Level).Entities)
            {
                if (entity is TrackSpinner tSpinner && tSpinner.Collider.Bounds.Intersects(Hitbox) && !dead)
                {
                    tSpinner.RemoveSelf();

                    Kill();

                    return;
                }

                if (entity is RotateSpinner rSpinner && rSpinner.Collider.Bounds.Intersects(Hitbox) && !dead)
                {
                    rSpinner.RemoveSelf();

                    Kill();

                    return;
                }

                if (entity is Bumper bumper && bumper.Collider.Bounds.Intersects(Hitbox) && !dead && !alreadyBouncedOffOf.Contains(bumper))
                {
                    if ((bool)Guneline.bumperFireMode.GetValue(bumper))
                    {
                        Kill();

                        return;
                    }
                    else
                    {
                        velocity = BootlegBumperHit(bumper);
                    }

                    alreadyBouncedOffOf.Add(bumper);
                }

                if (entity is Strawberry strawberry && strawberry.Golden && strawberry.Collider.Bounds.Intersects(Hitbox) && !dead)
                {
                    if (owner is Player pl)
                    {
                        pl.Die(Vector2.Zero, true);
                    }

                    Kill();

                    return;
                }

                if (entity is SummitGem sGem && sGem.Collider.Bounds.Intersects(Hitbox) && !dead && owner is Player p)
                {
                    sGem.Add(new Coroutine((IEnumerator)Guneline.summitGemSmashRoutine.Invoke(sGem, new object[] { p, p.Scene as Level })));
                }

                if (entity is Puffer puffer && puffer.Collider.Bounds.Intersects(Hitbox) && !dead)
                {
                    Guneline.pufferExplode.Invoke(puffer, null);

                    Guneline.pufferGotoGone.Invoke(puffer, null);

                    Kill();

                    return;
                }
            }

            if (owner.Scene.CollideFirst<StrawberrySeed>(Hitbox) is StrawberrySeed seed && !dead)
            {
                if (owner is Player p)
                {
                    Guneline.strawberrySeedOnPlayer.Invoke(seed, new object[] { p });
                }

                Kill();

                return;
            }

            if (owner.Scene.CollideFirst<Solid>(Hitbox) is Solid solid && !dead)
            {
                if (solid is DreamBlock dreamBlock)
                {
                    if ((owner.Scene as Level).Session.Inventory.DreamDash)
                    {
                        return;
                    }
                }

                if (solid is CrushBlock cBlock && owner is Player p)
                {
                    if (Center.Y > cBlock.Center.Y + (cBlock.Height / 2))
                    {
                        cBlock.OnDashCollide(p, -Vector2.UnitY);
                    }
                    else if (Center.Y < cBlock.Center.Y - (cBlock.Height / 2))
                    {
                        cBlock.OnDashCollide(p, Vector2.UnitY);
                    }
                    else if (Center.X > cBlock.Center.X + (cBlock.Width / 2))
                    {
                        cBlock.OnDashCollide(p, -Vector2.UnitX);
                    }
                    else if (Center.X < cBlock.Center.X - (cBlock.Width / 2))
                    {
                        cBlock.OnDashCollide(p, Vector2.UnitX);
                    }
                    else
                    {
                        cBlock.OnDashCollide(p, velocity);
                    }
                }

                if (solid is LightningBreakerBox lBBox && owner is Player pl)
                {
                    lBBox.Dashed(pl, velocity);
                }

                if (solid is DashSwitch dSwitch)
                {
                    dSwitch.OnDashCollide(null, (Vector2)Guneline.dashSwitchPressDirection.GetValue(dSwitch));
                }

                if (solid is FallingBlock fallingBlock)
                {
                    fallingBlock.Triggered = true;
                }

                Kill();

                return;
            }
        }

        private void Kill()
        {
            if (ValidTracker(owner))
            {
                for (int i = 0; i < 10; i++)
                {
                    (owner.Scene as Level).Particles.Emit(ParticleTypes.Dust, Position + Calc.Random.ShakeVector(), Color.Lerp(Color.Red, Color.Yellow, Calc.Random.NextFloat()));
                }
            }

            dead = true;

            RemoveSelf();
        }

        private bool BootlegStunSeeker(Seeker seeker)
        {
            if (!seeker.Regenerating)
            {
                Audio.Play("event:/game/05_mirror_temple/seeker_death", seeker.Position);

                Guneline.seekerDead.SetValue(seeker, true);

                Guneline.seekerGotBouncedOn.Invoke(seeker, new object[] { this });

                return true;
            }

            return false;
        }

        private void BootlegOshiroBounce(AngryOshiro oshiro)
        {
            Audio.Play("event:/game/general/thing_booped", oshiro.Position);

            Celeste.Celeste.Freeze(0.2f);

            ((StateMachine)Guneline.oshiroStateMachine.GetValue(oshiro)).State = 5;

            ((SoundSource)Guneline.oshiroPreChargeSFX.GetValue(oshiro)).Stop();

            ((SoundSource)Guneline.oshiroChargeSFX.GetValue(oshiro)).Stop();
        }

        private Vector2 BootlegBumperHit(Bumper bumper)
        {
            Level level = bumper.Scene as Level;

            if (level.Session.Area.ID == 9)
            {
                Audio.Play("event:/game/09_core/pinballbumper_hit", bumper.Position);
            }
            else
            {
                Audio.Play("event:/game/06_reflection/pinballbumper_hit", bumper.Position);
            }

            Guneline.bumperRespawnTimer.SetValue(bumper, 0.6f);

            Vector2 vector = (Center - bumper.Center).SafeNormalize();

            ((Sprite)Guneline.bumperSprite.GetValue(bumper)).Play("hit", restart: true);

            ((Sprite)Guneline.bumperSpriteEvil.GetValue(bumper)).Play("hit", restart: true);

            ((VertexLight)Guneline.bumperLight.GetValue(bumper)).Visible = false;

            ((BloomPoint)Guneline.bumperBloom.GetValue(bumper)).Visible = false;

            level.DirectionalShake(vector, 0.15f);

            level.Displacement.AddBurst(Center, 0.3f, 8, 32, 0.8f);

            level.Particles.Emit(Bumper.P_Launch, 12, Center + vector * 12, Vector2.One * 3, vector.Angle());

            return vector;
        }

        public static bool ValidTracker(Actor owner) 
            => owner != null && owner.Scene != null && owner.Scene.Tracker != null;
    }
}