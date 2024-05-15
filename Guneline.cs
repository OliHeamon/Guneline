using System;
using System.Linq;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Guneline
{
    public class Guneline : EverestModule
    {
        public static Guneline Instance { get; private set; }

        public override Type SettingsType => typeof(GunSettings);

        public static GunSettings Settings => (GunSettings)Instance._Settings;

        public Guneline()
        {
            Instance = this;
        }

        public static MTexture bulletTex;

        public static MTexture cartridgeTex;

        public static FieldInfo seekerDead;
        public static FieldInfo oshiroStateMachine;
        public static FieldInfo oshiroPreChargeSFX;
        public static FieldInfo oshiroChargeSFX;
        public static FieldInfo dashSwitchPressDirection;
        public static FieldInfo bumperFireMode;
        public static FieldInfo bumperRespawnTimer;
        public static FieldInfo bumperSprite;
        public static FieldInfo bumperSpriteEvil;
        public static FieldInfo bumperLight;
        public static FieldInfo bumperBloom;

        public static MethodInfo seekerGotBouncedOn;
        public static MethodInfo heartGemCollect;
        public static MethodInfo strawberrySeedOnPlayer;
        public static MethodInfo summitGemSmashRoutine;
        public static MethodInfo pufferExplode;
        public static MethodInfo pufferGotoGone;

        private bool celesteNetInstalled;

        private static Vector2 CursorPos => GunInput.CursorPosition;

        private bool GunWasShot => GunInput.GunShot;

        private Texture2D aimTex;

        private MTexture gunTexture;

        private int shotCooldown;

        private Level level;

        private const float PiOver8 = MathHelper.PiOver4 / 2;

        public bool GunToggledByTrigger { get; internal set; } = true;

        internal bool gunJustEquipped;

        public override void LoadContent(bool firstLoad)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            Type type = typeof(Seeker);

            seekerDead = type.GetField("dead", flags);

            seekerGotBouncedOn = type.GetMethod("GotBouncedOn", flags);

            type = typeof(AngryOshiro);

            oshiroStateMachine = type.GetField("state", flags);

            oshiroPreChargeSFX = type.GetField("prechargeSfx", flags);

            oshiroChargeSFX = type.GetField("chargeSfx", flags);

            heartGemCollect = typeof(HeartGem).GetMethod("Collect", flags);

            dashSwitchPressDirection = typeof(DashSwitch).GetField("pressDirection", flags);

            strawberrySeedOnPlayer = typeof(StrawberrySeed).GetMethod("OnPlayer", flags);

            type = typeof(Bumper);

            bumperFireMode = type.GetField("fireMode", flags);

            bumperRespawnTimer = type.GetField("respawnTimer", flags);

            bumperSprite = type.GetField("sprite", flags);

            bumperSpriteEvil = type.GetField("spriteEvil", flags);

            bumperLight = type.GetField("light", flags);

            bumperBloom = type.GetField("bloom", flags);

            summitGemSmashRoutine = typeof(SummitGem).GetMethod("SmashRoutine", flags);

            type = typeof(Puffer);

            pufferExplode = type.GetMethod("Explode", flags);

            pufferGotoGone = type.GetMethod("GotoGone", flags);

            aimTex = GFX.Game["DontConflictPlease/AimIndicator"].Texture.Texture;
            bulletTex = GFX.Game["DontConflictPlease/Bullet"];
            cartridgeTex = GFX.Game["DontConflictPlease/Cartridge"];
            gunTexture = GFX.Game["DontConflictPlease/Gun"];
        }


        public override void OnInputInitialize()
        {
            base.OnInputInitialize();
            GunInput.RegisterInputs();
        }

        public override void OnInputDeregister()
        {
            base.OnInputDeregister();
            GunInput.DeregisterInputs();
        }

        public override void Load()
        {
            On.Celeste.Player.Update += PlayerUpdated;
            On.Celeste.Player.Render += PlayerRendered;
            On.Celeste.Level.AfterRender += LevelRendered;

            Everest.Events.Level.OnLoadLevel += (level, playerIntro, isFromLoader) => {
                this.level = level;

                GunInput.CursorPosition = Vector2.Zero;
            };
        }

        public override void Initialize()
        {
            celesteNetInstalled = Everest.Modules.Any(mod => mod.GetType().Name.Equals("CelesteNetClientModule"));

            if (celesteNetInstalled)
            {
                DoCNetLoad(false);
            }
        }

        private void DoCNetLoad(bool unload)
        {
            if (unload)
            {
                CelesteNetClientContext.OnInit -= OnCelesteNetInit;
            }
            else
            {
                CelesteNetClientContext.OnInit += OnCelesteNetInit;
            }
        }

        public override void Unload()
        {
            DoCNetLoad(true);

            bulletTex = null;
            seekerDead = null;
            seekerGotBouncedOn = null;
            oshiroStateMachine = null;
            oshiroPreChargeSFX = null;
            oshiroChargeSFX = null;
            heartGemCollect = null;
            dashSwitchPressDirection = null;
            strawberrySeedOnPlayer = null;
            bumperFireMode = null;
            bumperRespawnTimer = null;
            bumperSprite = null;
            bumperSpriteEvil = null;
            bumperLight = null;
            bumperBloom = null;
            summitGemSmashRoutine = null;
            pufferExplode = null;
            pufferGotoGone = null;

            On.Celeste.Player.Update -= PlayerUpdated;
            On.Celeste.Player.Render -= PlayerRendered;
            On.Celeste.Level.AfterRender -= LevelRendered;
        }

        private void PlayerUpdated(On.Celeste.Player.orig_Update orig, Player self)
        {
            if (gunJustEquipped)
            {
                Audio.Play("event:/gunequipped", self.Center);
                gunJustEquipped = false;

                for (int i = 0; i < 8; i++)
                {
                    (self.Scene as Level).Particles.Emit(ParticleTypes.Dust, self.Center, Color.Yellow);
                }
            }

            if (Settings.GunEnabled && GunToggledByTrigger)
            {
                GunInput.UpdateInput(self);

                if (shotCooldown > 0)
                {
                    shotCooldown--;
                }

                if (self.Scene?.TimeActive > 0 && GunWasShot && shotCooldown == 0 && (TalkComponent.PlayerOver == null || !Input.Talk.Pressed))
                {
                    if (celesteNetInstalled)
                    {
                        Gunshot(self, CursorPos, self.Facing);

                        CelesteNetSendGunshot(self);
                    }
                    else
                    {
                        Gunshot(self, CursorPos);
                    }

                    SpriteEffects effects = SpriteEffects.None; // Not needed but gotta give the method all its params
                    (self.Scene as Level).DirectionalShake(GetGunVector(self, ref effects, CursorPos, self.Facing) / 5);

                    shotCooldown = 15;
                }
            }

            orig(self);
        }

        private void PlayerRendered(On.Celeste.Player.orig_Render orig, Player self)
        {
            orig(self);

            if (Settings.GunEnabled && GunToggledByTrigger)
            {
                RenderGun(self, self.Facing);
            }
        }

        private void LevelRendered(On.Celeste.Level.orig_AfterRender orig, Level self)
        {
            if (Settings.GunEnabled && GunToggledByTrigger)
            {
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);

                Draw.SpriteBatch.Draw(aimTex, CursorPos, null, Color.White, 0, new Vector2(aimTex.Width / 2, aimTex.Height / 2), 4, SpriteEffects.None, 0);

                Draw.SpriteBatch.End();
            }

            orig(self);
        }

        private void RenderGun(Actor player, Facings facing, Vector2? overrideCursorPos = null)
        {
            SpriteEffects effects = SpriteEffects.None;

            Vector2 gunVector = GetGunVector(player, ref effects, overrideCursorPos == null ? CursorPos : (Vector2)overrideCursorPos, facing);

            gunTexture.DrawCentered(player.Center, Color.White, 1, gunVector.ToRotation(), effects);
        }

        private static Vector2 GetGunVector(Actor player, ref SpriteEffects effects, Vector2 cursorPos, Facings forceDir)
        {
            float rotation = ToCursor(player, cursorPos).ToRotation();

            if (forceDir == Facings.Right)
            {
                rotation = MathHelper.Clamp(rotation, -MathHelper.PiOver4 - PiOver8, MathHelper.PiOver4 + PiOver8);
            }
            else
            {
                if (float.IsNaN(rotation))
                {
                    rotation = 0;
                }

                if (Math.Sign(rotation) == 1 && rotation < MathHelper.Pi - MathHelper.PiOver4 - PiOver8)
                {
                    rotation = MathHelper.Pi - MathHelper.PiOver4 - PiOver8;
                }
                if (Math.Sign(rotation) == -1 && rotation > -MathHelper.Pi + MathHelper.PiOver4 + PiOver8)
                {
                    rotation = -MathHelper.Pi + MathHelper.PiOver4 + PiOver8;
                }

                effects = SpriteEffects.FlipVertically;
            }

            return ToCursor(player, cursorPos).RotateTowards(rotation, MathHelper.TwoPi);
        }

        private static int ToDirectionInt(float x, float compareTo)
            => x > compareTo ? -1 : 1;

        private static Vector2 PlayerPosScreenSpace(Actor self)
            => self.Center - (self.Scene as Level).Camera.Position;

        private static Vector2 ToCursor(Actor player, Vector2 cursorPos)
            => Vector2.Normalize((cursorPos / 6) - PlayerPosScreenSpace(player));

        public static void Gunshot(Actor actor, Vector2 cursorPos, Facings facing = Facings.Left)
        {
            if (actor == null || actor.Scene == null)
            {
                return;
            }

            Vector2 actualPlayerPos = actor.Center;

            Vector2 ssPos = PlayerPosScreenSpace(actor);

            if (actor is Player player)
            {
                player.Facing = (Facings)ToDirectionInt(ssPos.X, cursorPos.X / 6);

                facing = player.Facing;
            }

            SpriteEffects effects = SpriteEffects.None;

            new Bullet(actualPlayerPos, GetGunVector(actor, ref effects, cursorPos, facing), actor);

            Audio.Play("event:/gunshot", actualPlayerPos);

            new Cartridge(actualPlayerPos, new Vector2(Calc.Random.Next(-2, 3), -Calc.Random.Next(3, 6)), actor);
        }

        private void CelesteNetSendGunshot(Player self)
        {
            CelesteNetClientModule.Instance.Context?.Client?.Send(new CelesteNetGunshotData()
            {
                Player = CelesteNetClientModule.Instance?.Client?.PlayerInfo,
                CursorPos = CursorPos,
                Facing = (int)self.Facing
            });
        }

        public void Handle(CelesteNetConnection con, CelesteNetGunshotData gData)
        {
            CelesteNetClientModule.Instance.Context.Main.Ghosts.TryGetValue(gData.Player.ID, out Ghost ghost);

            if (Settings.GunEnabled && GunToggledByTrigger && ghost != null && gData.Player.ID != CelesteNetClientModule.Instance.Client.PlayerInfo.ID && !level.Paused && level.Overlay == null)
            {
                Gunshot(ghost, gData.CursorPos, (Facings)gData.Facing);
            }
        }

        public void OnCelesteNetInit(CelesteNetClientContext ctx)
        {
            ctx.Client.Data.RegisterHandlersIn(this);
        }
    }
}
