using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShawLib
{
    public class WebServer : IDisposable
    {
        HttpListener listener;
        Action<HttpListenerContext> method;
        Thread thread;
        bool disposed;

        /// <summary>
        /// Initalize like new WebServer(answer, "http://*:80/name/");
        /// </summary>
        /// <param name="responseMethod">Function which  handles the request</param>
        /// <param name="prefixes">Urls where the webserver will be accessable on</param>
        public WebServer(Action<HttpListenerContext> responseMethod, params string[] prefixes)
        {
            method = responseMethod;
            listener = new HttpListener();

            if (!HttpListener.IsSupported)
                throw new NotSupportedException("WebServer is not supported on this operating system");

            foreach (var prefix in prefixes)
                listener.Prefixes.Add(prefix);
        }

        public void Start()
        {
            if (thread != null)
                thread.Abort();

            if (listener.IsListening)
                listener.Stop();
            listener.Start();

            thread = new Thread(run);
            thread.Start();
        }

        void run()
        {
            while (listener.IsListening)
            {
                try
                {
                    new Task((c) =>
                    {
                        var context = (HttpListenerContext)c;

                        try
                        {
                            method(context);
                        }
                        catch (HttpListenerException)
                        {
                            // throws if someone exit page during loading
                        }
                        finally
                        {
                            //context.Response.Close();
                        }
                    }, listener.GetContext()).Start();
                }
                catch (HttpListenerException)
                {
                    // throws if shutting down the webserver because listener.GetContext() aborts
                }
            }
        }

        public void Stop()
        {
            listener.Stop();
        }

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
                if (listener != null)
                    listener.Close();
                if (thread != null)
                    thread.Abort();
            }

            disposed = true;
        }
    }
}
