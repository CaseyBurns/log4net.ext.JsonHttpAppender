using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using log4net.Config;
using Newtonsoft.Json;
using Xunit;

namespace log4net.ext.JsonHttpAppender.Tests
{
    public class JsonHttpAppenderTests
    {
        public JsonHttpAppenderTests()
        {
            //xUnit overwrites listeners :(
            Trace.Listeners.Add(new DefaultTraceListener());
            Trace.Listeners.Add(new TextWriterTraceListener(@"log4net.ext.JsonHttpAppender.Tests.trace.log"));
        }

        [Fact]
        public void GivenValidConfig_ShouldLog_Test()
        {
            XmlConfigurator.Configure();
            var logger = LogManager.GetLogger(this.GetType());

            using (var server = new TestHttpServer())
            {
                server.Start(ConfigurationManager.AppSettings["httpListenerUrl"]);
                var task = server.WaitForRequestAsync();

                logger.Error("Failure!!!");

                var result = task.Wait(500);
                result.Should().BeTrue("we expect to receive a HTTP request");

                string requestBody = server.GetRequestBody(task.Result);
                server.SendResponse(task.Result, 200);

                var obj = JsonConvert.DeserializeObject(requestBody);
            }
        }

        [Fact]
        public void GivenUrlEmpty_ShouldNOTLog_Test()
        {
            LogManager.Shutdown();
            ILog logger = LoggerFactory.CreateLoggerWithJsonHttpAppender(GetType(), url: "");

            using (var server = new TestHttpServer())
            {
                server.Start(ConfigurationManager.AppSettings["httpListenerUrl"], 500);
                var task = server.WaitForRequestAsync();

                logger.Error("Failure!!!");

                var result = task.Wait(500);
                result.Should().BeFalse("HTTP request was not expected");
            }
        }
        
        public Task<string> HttpListenForOneRequest()
        {
            return new Task<string>(() =>
            {
                var url = ConfigurationManager.AppSettings["httpListenerUrl"];

                var listener = new HttpListener();
                listener.Prefixes.Add(url);
                listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                listener.TimeoutManager.IdleConnection = TimeSpan.FromMilliseconds(500);
                listener.Start();
                
                HttpListenerContext context = listener.GetContextAsync().Result;

                string requestContent;
                using (var r = new StreamReader(context.Request.InputStream))
                {
                    requestContent = r.ReadToEnd();
                }

                context.Response.StatusCode = 200;
                context.Response.Close();
                listener.Stop();

                return requestContent;
            });
        }
    }
}
