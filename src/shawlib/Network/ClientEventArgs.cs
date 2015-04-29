using System;

namespace ShawLib.Network
{
    public class ClientEventArgs : EventArgs
    {
        public TcpClient Client { get; private set; }

        public ClientEventArgs(TcpClient client)
        {
            Client = client;
        }
    }
}
