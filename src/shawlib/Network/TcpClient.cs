using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ShawLib;

namespace ShawLib.Network
{
    public class TcpClient : IDisposable
    {
        public IPAddress IP { get { return ((IPEndPoint)client.RemoteEndPoint).Address; } }
        public int Port { get { return ((IPEndPoint)client.RemoteEndPoint).Port; } }
        public bool Connected { get { return client != null ? client.Connected : false; } }

        public event EventHandler<EventArgs> OnConnect;
        public event EventHandler<ExceptionEventArgs> OnConnectFailed;
        public event EventHandler<ExceptionEventArgs> OnDisconnect;
        public event EventHandler<ReceiveEventArgs> OnReceive;

        Socket client;
        Thread threadReceive;
        bool disposed;

        /// <summary>
        /// Creates a new client instance
        /// </summary>
        public TcpClient()
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        { }

        // used by the client class and server class
        internal TcpClient(Socket client)
        {
            this.client = client;
        }

        void onConnectEvent()
        {
            var handler = OnConnect;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        void onConnectFailedEvent(Exception ex)
        {
            var handler = OnConnectFailed;
            if (handler != null)
                handler(this, new ExceptionEventArgs(ex));
        }

        void onDisconnectEvent(Exception ex)
        {
            var handler = OnDisconnect;
            if (handler != null)
                handler(this, new ExceptionEventArgs(ex));
            Disconnect();
        }

        void onReceiveEvent(byte[] bytes)
        {
            var handler = OnReceive;
            if (handler != null)
                handler(this, new ReceiveEventArgs(this, bytes));
        }

        void receive()
        {
            var lengthBytes = new byte[4];
            byte[] buffer;

            try
            {
                while (client != null)
                {
                    // Receive packet length
                    if (client.Receive(lengthBytes) != lengthBytes.Length)
                        throw new Exception("Disconnected");

                    var length = BitConverter.ToInt32(lengthBytes, 0);
                    if (length <= 0)
                        throw new Exception("packet length below zero");
                    else if (length > client.ReceiveBufferSize - lengthBytes.Length)
                        throw new Exception("packet length above limit");

                    // Receive packet bytes
                    buffer = new byte[length];
                    if (client.Receive(buffer) != length)
                        throw new Exception("Disconnected, packet length did not match");

                    // Enqueue bytes into receive queue
                    onReceiveEvent(buffer);
                }
            }
            catch (Exception ex)
            {
                onDisconnectEvent(ex);
            }
        }

        /// <summary>
        /// Connects to a server
        /// </summary>
        /// <param name="ip">The server ip address</param>
        /// <param name="port">The server port</param>
        public void Connect(IPAddress ip, int port, int timeout = 3000)
        {
            if (client == null)
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // only be able to bind one client to the port
            if (!client.Connected && !client.IsBound)
                client.ExclusiveAddressUse = true;

            // rise the maximun buffer, this allow bigger packets up to 
            client.ReceiveBufferSize = int.MaxValue;
            client.SendBufferSize = int.MaxValue;

            bool success = false;

            try
            {
                // start connecting with a timeout of three seconds
                var result = client.BeginConnect(new IPEndPoint(ip, port), null, null);
                success = result.AsyncWaitHandle.WaitOne(timeout);

                if (success)
                    client.EndConnect(result);
            }
            catch (Exception ex)
            {
                onConnectFailedEvent(ex);
                return;
            }

            // Timeout
            if (!success)
            {
                onConnectFailedEvent(new TimeoutException());
                return;
            }

            Start();
            onConnectEvent();
        }

        public void Disconnect()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
        }

        internal void Start()
        {
            threadReceive = new Thread(receive);
            threadReceive.IsBackground = true;
            threadReceive.Start();
        }

        /// <summary>
        /// Sends bytes over the network stream
        /// </summary>
        /// <param name="bytes">The bytes to be sent</param>
        /// <returns></returns>
        public bool Send(byte[] bytes)
        {
            if (!client.Connected && client != null)
                return false;

            if (bytes.Length + 4 > client.SendBufferSize)
                throw new Exception("packet length above limit");

            // Send the bytes of the packet over the network stream
            bytes = BitConverter.GetBytes(bytes.Length).Add(bytes);
            try
            {
                client.Send(bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Diposes the client
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
                Disconnect();

            disposed = true;
        }
    }
}
