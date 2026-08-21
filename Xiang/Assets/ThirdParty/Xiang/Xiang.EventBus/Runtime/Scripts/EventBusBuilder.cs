using System;
using Xiang.EventBus.Core;

namespace Xiang.EventBus.Runtime
{
    /// <summary>
    /// Builds a ready-to-use <see cref="EventBusContext"/> from an <see cref="EventBusConfiguration"/>.
    /// The VitalRouter backend is only constructed by the VitalRouter integration assembly; here, the
    /// builder resolves the backend through an injected factory so Core/Runtime never reference
    /// VitalRouter directly.
    /// </summary>
    public sealed class EventBusBuilder
    {
        private EventBusConfiguration _configuration = EventBusConfiguration.Default;
        private Func<EventBusConfiguration, ICommandPublisher> _commandPublisherFactory;

        public EventBusBuilder WithConfiguration(EventBusConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            return this;
        }

        /// <summary>
        /// Installs a custom command-publisher factory (for example the VitalRouter adapter). If none
        /// is set, the builder falls back to <see cref="InProcessCommandPublisher"/>.
        /// </summary>
        public EventBusBuilder WithCommandPublisherFactory(
            Func<EventBusConfiguration, ICommandPublisher> factory)
        {
            _commandPublisherFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            return this;
        }

        public EventBusContext Build()
        {
            ICommandPublisher commandPublisher = _commandPublisherFactory != null
                ? _commandPublisherFactory(_configuration)
                : new InProcessCommandPublisher(
                    _configuration.CommandQueueCapacity,
                    _configuration.CommandOverflowPolicy);

            // Construction of the context never fails after resources are created, so no rollback is
            // needed beyond disposing the publisher if an unexpected error occurs.
            try
            {
                return new EventBusContext(_configuration, commandPublisher);
            }
            catch
            {
                if (commandPublisher is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                throw;
            }
        }
    }
}
