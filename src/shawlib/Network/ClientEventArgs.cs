using System;

namespace BKR
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
