using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using log4net.Appender;
using System.IO;
using System.Net;
using log4net.Core;
using Newtonsoft.Json;

namespace log4net.ext.JsonHttpAppender
{
    public class JsonHttpAppender : AppenderSkeleton
    {
        public string Url { get; set; }

        protected override async void Append(LoggingEvent loggingEvent)
        {
            if (ValidateProperties()) return;

            var loggingMessage = CreateLogMessage(loggingEvent);

            var response = await SendHttpRequest(loggingMessage);

            if (!new[] {HttpStatusCode.Created, HttpStatusCode.OK}.Contains(response.StatusCode))
            {
                TraceWriteIfDebugEnabled("Received '{0}' from '{1}'", response.StatusCode, Url);
            }
        }

        private async Task<HttpWebResponse> SendHttpRequest(string loggingMessage)
        {
            HttpWebResponse response;
            try
            {
                var request = WebRequest.Create(Url);

                request.Method = "POST";
                request.ContentType = "application/json";
                using (var w = new StreamWriter(request.GetRequestStream()))
                {
                    w.Write(loggingMessage);
                }

                response = (HttpWebResponse) await request.GetResponseAsync();
            }
            catch (Exception ex)
            {
                TraceWriteIfDebugEnabled("Failed to POST to '{0}': '{1}'", Url, ex.ToString());
                throw;
            }
            return response;
        }

        private string CreateLogMessage(LoggingEvent loggingEvent)
        {            
            try
            {
                return JsonConvert.SerializeObject(new
                {
                    Server = Environment.MachineName,
                    ExceptionStackTrace = loggingEvent.ExceptionObject != null ? loggingEvent.ExceptionObject.ToString() : "",
                    ExceptionType = loggingEvent.ExceptionObject != null ? loggingEvent.ExceptionObject.GetType().Name : "",
                    Level = loggingEvent.Level.DisplayName,
                    Message = RenderLoggingEvent(loggingEvent),
                    loggingEvent.UserName,
                    loggingEvent.TimeStamp,
                    loggingEvent.LocationInformation,
                    loggingEvent.LoggerName,
                    loggingEvent.ThreadName,
                });
            }
            catch (Exception ex)
            {
                TraceWriteIfDebugEnabled("Failed to deserialze to json: ", ex.ToString());
                throw;
            }
        }

        private bool ValidateProperties()
        {
            if (!Uri.IsWellFormedUriString(Url, UriKind.Absolute))
            {
                TraceWriteIfDebugEnabled("Configured url '{0}' is not valid." +
                                         "\n\tFor example:" +
                                         "\n\t<appender name=\"JsonHttpAppender\" type=\"log4net.ext.JsonHttpAppender.JsonHttpAppender, log4net.ext.JsonHttpAppender\">" +
                                         "\n\t\t<url value=\"http://blah.com/log\"/>" +
                                         "\n\t\t<layout type=\"log4net.Layout.SimpleLayout\" />" +
                                         "\n\t</appender>",
                    Url);
                return true;
            }
            return false;
        }

        protected override bool RequiresLayout
        {
            get { return true; }
        }

        private void TraceWrite(string message, params object[] args)
        {
            if (args != null)
                message = string.Format(message, args);
            Trace.WriteLine("log4net: JsonHttpAppender: " + message);
        }

        private void TraceWriteIfDebugEnabled(string message, params object[] args)
        {
            if (
                !string.Equals(ConfigurationManager.AppSettings["log4net.Internal.Debug"], bool.TrueString,
                    StringComparison.InvariantCultureIgnoreCase))
                return;

            TraceWrite(message, args);

        }
    }
}
