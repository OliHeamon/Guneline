using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;

namespace Guneline
{
    public class CelesteNetGunshotData : DataType<CelesteNetGunshotData>
    {
        public DataPlayerInfo Player;
        public Vector2 CursorPos;
        public int Facing;

        static CelesteNetGunshotData()
        {
            DataID = "gunmodShot";
        }

        protected override void Read(CelesteNetBinaryReader reader)
        {
            Facing = reader.ReadInt32();
            CursorPos = reader.ReadVector2();
        }

        protected override void Write(CelesteNetBinaryWriter writer)
        {
            writer.Write(Facing);
            writer.Write(CursorPos);
        }

        public override MetaType[] GenerateMeta(DataContext ctx)
            => new MetaType[] { new MetaPlayerUpdate(Player) };

        public override void FixupMeta(DataContext ctx)
        {
            Player = Get<MetaPlayerUpdate>(ctx);
        }
    }
}
