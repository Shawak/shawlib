using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace ShawLib.Packet
{
    internal class PacketManager
    {
        static Dictionary<ushort, Type> packetTypes = new Dictionary<ushort, Type>();
        static Dictionary<Type, ushort> packetIDs = new Dictionary<Type, ushort>();

        public static void Initalize(Type[] packetTypes)
        {
            for (ushort i = 0; i < packetTypes.Length; i++)
            {
                if (!PacketManager.packetTypes.ContainsValue(packetTypes[i]))
                {
                    PacketManager.packetTypes.Add(i, packetTypes[i]);
                    packetIDs.Add(packetTypes[i], i);
                }
            }
        }

        public static void Explore(Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(IPacket)))
                .OrderBy(x => x.Name).ToArray();

            Initalize(types);
        }

        public static string Show()
        {
            var sb = new StringBuilder();
            foreach (var packet in packetTypes)
                sb.AppendLine(packet.Key + " - " + packet.Value.Name);
            return sb.ToString();
        }

        public static byte[] Pack(IPacket packet)
        {
            var stream = new MemoryStream();
            var packetID = packetIDs[packet.GetType()];
            stream.Add(packetID);
            packet.Pack(ref stream);
            return stream.ToArray();
        }

        public static IPacket Parse(byte[] data)
        {
            var stream = new MemoryStream(data);
            var packetID = stream.Get<ushort>();
            var packetType = packetTypes[packetID];
            var packet = (IPacket)FormatterServices.GetUninitializedObject(packetType);
            packet.Parse(stream);
            return packet;
        }
    }
}
