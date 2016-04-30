using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ShawLib.TCP
{
    public class TcpServer : IDisposable
    {
        public IPAddress IP { get; private set; }
        public int Port { get; private set; }

        public event EventHandler<ClientEventArgs> OnClientConnect;
        public event EventHandler<ClientEventArgs> OnClientDisconnect;

        Socket listener;
        bool disposed;
        Thread threadAcceptSockts;

        /// <summary>
        /// Creates a new server instance 
        /// </summary>
        public TcpServer()
        { }

        void onClientConnectEvent(TcpClient client)
        {
            var handler = OnClientConnect;
            if (handler != null)
                handler(this, new ClientEventArgs(client));
        }

        void onClientDisconnectEvent(TcpClient client)
        {
            var handler = OnClientDisconnect;
            if (handler != null)
                handler(this, new ClientEventArgs(client));
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        /// <param name="ip">The ip the server in running on</param>
        /// <param name="port">The port the server is running on</param>
        public void Start(IPAddress ip, int port)
        {
            // Be sure to close the old instance
            Close();

            // Create a new listener and bind it to the ip/port
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(ip, port));
            listener.Listen(100);

            // Start accepting incoming sockets
            threadAcceptSockts = new Thread(acceptSockets);
            threadAcceptSockts.IsBackground = true;
            threadAcceptSockts.Start();
        }

        /// <summary>
        /// Closes the server and stops accepting clients
        /// </summary>
        public void Close()
        {
            // close the listner
            if (listener != null)
                listener.Close();

            // stop accepting new sockets
            if (threadAcceptSockts != null)
                threadAcceptSockts.Abort();
        }

        void acceptSockets()
        {
            // accept sockets while there is a listener
            while (listener != null)
            {
                // get the new socket, link the client events and start listening on that client
                var socket = listener.Accept();
                var client = new TcpClient(socket);
                client.OnDisconnect += client_OnDisconnect;
                onClientConnectEvent(client);
                client.Start();
            }
        }

        void client_OnDisconnect(object sender, EventArgs e)
        {
            onClientDisconnectEvent((TcpClient)sender);
        }

        /// <summary>
        /// Disposes the server instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                listener.Dispose();

            disposed = true;
        }
    }
}
