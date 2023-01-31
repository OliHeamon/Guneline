using Celeste.Mod;
using Microsoft.Xna.Framework.Input;

namespace GunelineMk2.Code
{
    public class GunelineSettings : EverestModuleSettings
    {
        [DefaultButtonBinding(Buttons.Y, Keys.S)]
        public ButtonBinding ShootButton { get; set; }
    }
}
