using System;

namespace ShawLib.TCP
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
