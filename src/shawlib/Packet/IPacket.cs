using System.IO;

namespace ShawLib.Packet
{
    public interface IPacket
    {
        void Pack(ref MemoryStream stream);
        void Parse(MemoryStream stream);
    }
}