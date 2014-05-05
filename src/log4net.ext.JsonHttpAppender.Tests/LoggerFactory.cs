using System;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace log4net.ext.JsonHttpAppender.Tests
{
    public class LoggerFactory
    {
        public static ILog CreateLoggerWithJsonHttpAppender(Type loggerForType, string url, ILayout layout = null)
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            hierarchy.Root.AddAppender(new JsonHttpAppender
            {
                Url = url,
                Layout = layout ?? new SimpleLayout(),
            });
            hierarchy.Root.Level = Level.All;
            hierarchy.Configured = true;
            
            var logger = LogManager.GetLogger(loggerForType);
            return logger;
        }
    }
}
