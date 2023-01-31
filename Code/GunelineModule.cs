using Celeste.Mod;
using System;

namespace GunelineMk2.Code
{
    public class GunelineModule : EverestModule
    {
        public static GunelineModule Instance { get; private set; }

        public static GunelineSettings Settings => (GunelineSettings)Instance._Settings;

        public override Type SettingsType => typeof(GunelineSettings);

        public override void Load()
        {
            Instance = this;
        }

        public override void Unload()
        {
        }
    }
}
