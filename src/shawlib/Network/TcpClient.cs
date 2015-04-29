using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShawLib.Network
{
    public class TcpClient : IDisposable
    {
        public IPAddress IP { get { return ((IPEndPoint)client.RemoteEndPoint).Address; } }
        public int Port { get { return ((IPEndPoint)client.RemoteEndPoint).Port; } }

        public event EventHandler<EventArgs> OnConnect;
        public event EventHandler<ExceptionEventArgs> OnConnectFailed;
        public event EventHandler<ExceptionEventArgs> OnDisconnect;
        public event EventHandler<ReceiveEventArgs> OnReceive;

        Socket client;
        Task taskConnect;
        bool disposed, stop;

        Queue<byte[]> queueReceive;
        Queue<byte[]> queueSend;

        object lock_queueReceive;
        object lock_queueSend;

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

            queueReceive = new Queue<byte[]>();
            queueSend = new Queue<byte[]>();

            lock_queueReceive = new object();
            lock_queueSend = new object();

            // only be able to bind one client to the port
            if (!client.Connected)
                client.ExclusiveAddressUse = true;

            // rise the maximun buffer, this allow bigger packets up to 
            client.ReceiveBufferSize = int.MaxValue;
            client.SendBufferSize = int.MaxValue;
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
        }

        void onReceiveEvent(byte[] bytes)
        {
            var handler = OnReceive;
            if (handler != null)
                handler(this, new ReceiveEventArgs(this, bytes));
        }

        void taskHandle()
        {
            while (client != null && !stop)
            {
                if (queueReceive.Count > 0)
                {
                    // get the latest received bytes and cann the receive event
                    byte[] bytes;
                    lock (lock_queueReceive)
                        bytes = queueReceive.Dequeue();

                    onReceiveEvent(bytes);
                }
                else
                    Thread.Sleep(1);
            }
        }

        void taskReceive()
        {
            var lengthBytes = new byte[4];
            byte[] buffer;

            try
            {
                while (client != null && !stop)
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
                        throw new Exception("Disconnected");

                    // Enqueue bytes into receive queue
                    lock (lock_queueReceive)
                        queueReceive.Enqueue(buffer);
                }
            }
            catch (Exception ex)
            {
                onDisconnectEvent(ex);
            }
        }

        void taskSend()
        {
            var lengthBytes = new byte[4];
            byte[] buffer;

            try
            {
                while (client != null && !stop)
                {
                    if (queueSend.Count > 0)
                    {
                        // dequeue the next packet which should be send
                        byte[] bytes;
                        lock (lock_queueSend)
                            bytes = queueSend.Dequeue();

                        if (bytes.Length + lengthBytes.Length > client.SendBufferSize)
                            throw new Exception("packet length above limit");

                        // Send the bytes of the packet over the network stream
                        lengthBytes = BitConverter.GetBytes(bytes.Length);
                        buffer = new byte[bytes.Length + lengthBytes.Length];
                        Buffer.BlockCopy(lengthBytes, 0, buffer, 0, lengthBytes.Length);
                        Buffer.BlockCopy(bytes, 0, buffer, lengthBytes.Length, bytes.Length);
                        client.Send(buffer);
                    }
                    else
                        Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                onDisconnectEvent(ex);
            }
        }

        /// <summary>
        /// Stats to connect to a server asynchronous
        /// </summary>
        /// <param name="ip">The server ip address</param>
        /// <param name="port">The server port</param>
        public void StartConnect(IPAddress ip, int port)
        {
            if (taskConnect != null && !taskConnect.IsCompleted)
                return;

            taskConnect = Task.Run(() => Connect(ip, port));
        }

        /// <summary>
        /// Connects to a server
        /// </summary>
        /// <param name="ip">The server ip address</param>
        /// <param name="port">The server port</param>
        public void Connect(IPAddress ip, int port, int timeout = 3000)
        {
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

        Thread[] threads;

        internal void Start()
        {
            // create new threads send/receive/handle
            threads = new Thread[] {
                new Thread(taskReceive) { IsBackground = true },
                new Thread(taskSend) { IsBackground = true },
                new Thread(taskHandle) { IsBackground = true }
            };

            // run these threads
            foreach (var thread in threads)
                thread.Start();
        }

        /// <summary>
        /// Sends bytes over the network stream
        /// </summary>
        /// <param name="bytes">The bytes to be sent</param>
        /// <returns></returns>
        public bool Send(byte[] bytes)
        {
            if (!client.Connected)
                return false;

            // enqueue the bytes into the send queue
            lock (lock_queueSend)
                queueSend.Enqueue(bytes);
            return true;
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
            {
                // wait for pending packets to be send
                while (queueSend.Count > 0)
                    Thread.Sleep(1);

                stop = true;
                threads[1].Join();
                client.Dispose();
            }

            disposed = true;
        }
    }
}
