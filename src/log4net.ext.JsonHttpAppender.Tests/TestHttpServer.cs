using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace log4net.ext.JsonHttpAppender.Tests
{
    public class TestHttpServer : IDisposable
    {
        private HttpListener _listener = new HttpListener();

        public void Start(string urlToListen, int millisecondsTimeout = 0)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(urlToListen);
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

            if (millisecondsTimeout > 0)
            {
                _listener.TimeoutManager.IdleConnection = TimeSpan.FromMilliseconds(millisecondsTimeout);
            }

            _listener.Start();
        }

        public void Stop()
        {
            if (_listener == null)
                return;

            _listener.Stop();
            (_listener as IDisposable).Dispose();
        }

        public Task<HttpListenerContext> WaitForRequestAsync()
        {
            return _listener.GetContextAsync();
        }

        public string GetRequestBody(HttpListenerContext context)
        {
            string requestBody;
            using (var r = new StreamReader(context.Request.InputStream))
            {
                requestBody = r.ReadToEnd();
            }
            return requestBody;
        }

        public void SendResponse(HttpListenerContext context, int responseCode)
        {
            context.Response.StatusCode = responseCode;
            context.Response.Close();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
