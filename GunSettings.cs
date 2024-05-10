using Celeste.Mod;
using Microsoft.Xna.Framework.Input;

namespace Guneline
{
    [SettingName("Guneline Settings")]
    public class GunSettings : EverestModuleSettings
    {
        [SettingName("Gun Enabled")]
        [SettingSubText("If this is off, you will also not recieve shots from CelesteNet players.\nNOTE: In addition to the settings below, you can also use Mouse1 to fire the gun.")]
        public bool GunEnabled { get; set; } = false;

        [SettingName("Fire Weapon")]
        [DefaultButtonBinding(Buttons.LeftTrigger, Keys.LeftShift)]
        public ButtonBinding ShootButton { get; set; }
    }
}
