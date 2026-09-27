using System.IO;
using CalamityMPFixes.Fixes;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityMPFixes
{
    public class CalamityMPFixes : Mod
    {
        internal enum MessageType : byte
        {
            WulfrumLureSpawnSparks
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            var messageType = (MessageType)reader.ReadByte();
            switch (messageType)
            {
                case MessageType.WulfrumLureSpawnSparks:
                    Vector2 position = reader.ReadVector2();
                    if (!Main.dedServ)
                        WulfrumLureFix.SpawnArrivalSparks(position);
                    break;

                default:
                    Logger.Warn($"Unknown message type: {messageType}");
                    break;
            }
        }
    }
}
