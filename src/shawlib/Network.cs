using ShawLib.Packet;
using ShawLib.TCP;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace ShawLib
{
    public class Network
    {
        public bool EnableConsoleWriting { get; set; }

        Dictionary<Type, Action<IPacket>> linker;
        TcpClient client;

        public Network(TcpClient client)
        {
            linker = new Dictionary<Type, Action<IPacket>>();
            this.client = client;
            this.client.OnReceive += client_OnReceive;
        }

        public static void Explore(Assembly assembly)
        {
            PacketManager.Explore(assembly);
        }

        public static string Show()
        {
            return PacketManager.Show();
        }

        public void Link<T>(Action<T> action) where T : IPacket
        {
            var type = typeof(T);
            if (linker.ContainsKey(type))
                throw new Exception(type.ToString() + " is already linked");

            if (EnableConsoleWriting)
                print("LINK " + type.Name);
            linker[type] = new Action<IPacket>(e => action((T)e));
        }

        public void Unlink<T>()
        {
            var type = typeof(T);
            if (!linker.ContainsKey(type))
            {
                if (EnableConsoleWriting)
                    print("ERR could not unlink " + type.Name + " (nothing to unlink)");
                return;
            }

            if (EnableConsoleWriting)
                print("UNLINK " + type.Name);
            linker.Remove(type);
        }

        void print(string str)
        {
            Console.WriteLine("[Network (" + client.IP + ")] " + str);
        }

        void client_OnReceive(object sender, ReceiveEventArgs e)
        {
            IPacket packet;
            try
            {
                packet = PacketManager.Parse(e.Bytes);
            }
            catch (Exception ex)
            {
                print("ERR could not parse packet, error: " + ex);
                return;
            }

            if (EnableConsoleWriting)
                print("RECV " + packet.GetType().Name);
            call(packet);
        }

        public bool Send(IPacket packet)
        {
            if (EnableConsoleWriting)
                print("SEND " + packet.GetType().Name);
            return client.Send(PacketManager.Pack(packet));
        }

        public T SendWait<T>(IPacket packet, int timeout = 5000) where T : IPacket
        {
            T ret = default(T);

            var handler = new ManualResetEvent(false);
            Link<T>(e =>
            {
                ret = e;
                handler.Set();
            });

            if (Send(packet))
                handler.WaitOne(timeout);

            Unlink<T>();
            return ret;
        }

        void call(IPacket packet)
        {
            var type = packet.GetType();
            if (!linker.ContainsKey(type))
            {
                if (EnableConsoleWriting)
                    print("DROP " + type.Name + " (no link available)");
                return;
            }
            linker[type].Invoke(packet);
        }
    }
}
