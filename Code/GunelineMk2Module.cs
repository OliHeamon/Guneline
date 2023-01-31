using Celeste.Mod;

namespace GunelineMk2.Code
{
    public class GunelineMk2Module : EverestModule
    {
        public static GunelineMk2Module Instance { get; private set; }

        public override void Load()
        {
            Instance = this;
        }

        public override void Unload()
        {
        }
    }
}
