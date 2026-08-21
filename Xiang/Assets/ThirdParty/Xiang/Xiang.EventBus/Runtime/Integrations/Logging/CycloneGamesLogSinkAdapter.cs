using System;
using System.Text;
using CycloneGames.Logging;
using Xiang.EventBus.Core;

namespace Xiang.EventBus.Runtime.Integrations.Logging
{
    /// <summary>
    /// Adapts the Core's narrow <see cref="IEventBusLogSink"/> port to CycloneGames.Logging.
    /// This is the only place CycloneGames.Logging types appear; the Core layer stays neutral.
    /// </summary>
    public sealed class CycloneGamesLogSinkAdapter : IEventBusLogSink
    {
        private readonly ILogWriter _writer;

        public CycloneGamesLogSinkAdapter(ILogWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public bool IsEnabled(EventBusLogSeverity severity, string category)
        {
            return LogChannel.Create(category, _writer).IsEnabled(Map(severity));
        }

        public void Write(
            EventBusLogSeverity severity,
            string category,
            Action<StringBuilder> messageBuilder)
        {
            LogChannel.Create(category, _writer).Write(Map(severity), messageBuilder);
        }

        public void WriteException(
            EventBusLogSeverity severity,
            string category,
            Exception exception,
            string message)
        {
            LogChannel.Create(category, _writer).WriteException(Map(severity), exception, message);
        }

        private static LogSeverity Map(EventBusLogSeverity severity)
        {
            switch (severity)
            {
                case EventBusLogSeverity.Debug:
                    return LogSeverity.Debug;
                case EventBusLogSeverity.Info:
                    return LogSeverity.Info;
                case EventBusLogSeverity.Warning:
                    return LogSeverity.Warning;
                case EventBusLogSeverity.Error:
                    return LogSeverity.Error;
                default:
                    return LogSeverity.Info;
            }
        }
    }
}
