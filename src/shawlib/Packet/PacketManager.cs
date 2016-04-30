using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace ShawLib.Packet
{
    public class PacketManager
    {
        static Dictionary<ushort, Type> packetTypes;
        static Dictionary<Type, ushort> packetIDs;

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
            packetTypes = new Dictionary<ushort, Type>();
            packetIDs = new Dictionary<Type, ushort>();

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
            // Create packet stream
            var stream = new MemoryStream();

            // Add packet type
            var packetID = packetIDs[packet.GetType()];
            stream.Add(packetID);

            // Add packet bytes
            packet.Pack(ref stream);

            // Return bytes
            return stream.ToArray();
        }

        public static IPacket Parse(byte[] data)
        {
            // Create packet stream
            var stream = new MemoryStream(data);

            // Get packet type
            var packetID = stream.Get<ushort>();
            var packetType = packetTypes[packetID];

            // Get packet bytes and turn it into a packet
            var packet = (IPacket)FormatterServices.GetUninitializedObject(packetType);

            // Parse packet
            packet.Parse(stream);
            return packet;
        }
    }
}
