using System;

namespace ShawLib.TCP
{
    public class ReceiveEventArgs : EventArgs
    {
        public TcpClient Client { get; private set; }
        public byte[] Bytes { get; private set; }

        public ReceiveEventArgs(TcpClient client, byte[] bytes)
        {
            Client = client;
            Bytes = bytes;
        }
    }
}
