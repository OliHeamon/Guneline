using Celeste.Mod;
using Microsoft.Xna.Framework.Input;

namespace Guneline
{
    public class GunSettings : EverestModuleSettings
    {
        [SettingSubText("MODOPTIONS_GUNELINE_GunEnabled_HINT")]
        public bool GunEnabled { get; set; } = false;

        #region Key Bindings

        [DefaultButtonBinding(Buttons.LeftTrigger, Keys.LeftShift)]
        public ButtonBinding ShootButton { get; set; }

        #endregion
    }
}
