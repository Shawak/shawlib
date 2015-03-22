using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShawLib
{
    public class WebServer
    {
        HttpListener listener;
        Func<HttpListenerRequest, string> method;

        /// <summary>
        /// Initalize like new WebServer(answer, "http://*:80/name/");
        /// </summary>
        /// <param name="responseMethod">Function which  handles the request</param>
        /// <param name="prefixes">Urls where the webserver will be accessable on</param>
        public WebServer(Func<HttpListenerRequest, string> responseMethod, params string[] prefixes)
        {
            method = responseMethod;
            listener = new HttpListener();

            if (!HttpListener.IsSupported)
                throw new NotSupportedException("WebServer is not supported on this operating system");

            foreach (var prefix in prefixes)
                listener.Prefixes.Add(prefix);

            listener.Start();
        }

        public void Run()
        {
            new Thread(threadRun).Start();
        }

        void threadRun()
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
                            var ret = method(context.Request);
                            var buffer = Encoding.UTF8.GetBytes(ret);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                        catch (HttpListenerException)
                        {
                            // throws if someone exit page during loading
                        }
                        finally
                        {
                            context.Response.Close();
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
            listener.Close();
        }
    }
}
