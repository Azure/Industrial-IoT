// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Threading.Tasks;

    /// <summary>
    /// Simple client that can be used to connect a session to a
    /// server and subscribe to data changes and events.
    /// </summary>
    public class ClientApplication
    {
        /// <summary>
        /// Gets or sets the data change callback.
        /// </summary>
        public DataChangeNotificationHandler? DataChangeCallback { get; set; }

        /// <summary>
        /// Gets or sets the event callback.
        /// </summary>
        public EventNotificationHandler? EventCallback { get; set; }

        /// <summary>
        /// Gets or sets the keep alive callback.
        /// </summary>
        public KeepAliveNotificationHandler? KeepAliveCallback { get; set; }

        /// <summary>
        /// Configuration
        /// </summary>
        public ApplicationConfiguration Configuration { get; }

        /// <summary>
        /// Logger factory
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Reverse connect manager
        /// </summary>
        public ReverseConnectManager? ReverseConnectManager { get; }

        /// <summary>
        /// Time provider
        /// </summary>
        public TimeProvider TimeProvider { get; }

        /// <summary>
        /// Create client appolication
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="reverseConnectManager"></param>
        /// <param name="timeProvider"></param>
        public ClientApplication(ApplicationConfiguration configuration,
            ILoggerFactory loggerFactory, ReverseConnectManager? reverseConnectManager = null,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            if (configuration.ClientConfiguration == null ||
                configuration.SecurityConfiguration == null ||
                configuration.CertificateValidator == null)
            {
                throw new ServiceResultException(StatusCodes.BadConfigurationError,
                    "The application configuration for the session is missing fields.");
            }

            Configuration = configuration;
            LoggerFactory = loggerFactory;
            ReverseConnectManager = reverseConnectManager;
            TimeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <summary>
        /// Create session
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual Session CreateSession(ConfiguredEndpoint endpoint,
            SessionOptions? options = null)
        {
            return new ClientSession(this, endpoint, options ?? new SessionOptions());
        }

        /// <summary>
        /// Default meter factory
        /// </summary>
        internal static IMeterFactory DefaultMeterFactory { get; } = new MeterFactory();

        /// <inheritdoc/>
        private sealed class ClientSession : Session
        {
            public ClientApplication Application { get; }

            /// <inheritdoc/>
            public ClientSession(ClientApplication application, ConfiguredEndpoint endpoint,
                SessionOptions options)
                : base(application.Configuration, endpoint, options, application.LoggerFactory,
                      application.TimeProvider, application.ReverseConnectManager)
            {
                Application = application;
            }

            /// <inheritdoc/>
            protected override Subscription CreateSubscription(SubscriptionOptions? options,
                IAckQueue queue)
            {
                return new ClientSubscription(this, queue,
                    LoggerFactory.CreateLogger<ClientSubscription>());
            }
        }

        /// <inheritdoc/>
        private sealed class ClientSubscription : Subscription
        {
            /// <inheritdoc/>
            public ClientSubscription(ClientSession session, IAckQueue completion,
                ILogger logger) : base(session, completion, logger)
            {
            }

            /// <inheritdoc/>
            protected override ValueTask OnDataChangeNotificationAsync(uint sequenceNumber,
                DateTime publishTime, DataChangeNotification notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                notification.PublishTime = publishTime;
                notification.SequenceNumber = sequenceNumber;
                (Session as ClientSession)?.Application.DataChangeCallback?.Invoke(
                    this, notification, stringTable);
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            protected override ValueTask OnEventDataNotificationAsync(uint sequenceNumber,
                DateTime publishTime, EventNotificationList notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                notification.PublishTime = publishTime;
                notification.SequenceNumber = sequenceNumber;
                (Session as ClientSession)?.Application.EventCallback?.Invoke(
                    this, notification, stringTable);
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            protected override ValueTask OnKeepAliveNotificationAsync(uint sequenceNumber,
                DateTime publishTime, PublishState publishStateMask)
            {
                (Session as ClientSession)?.Application.KeepAliveCallback?.Invoke(
                    this, new NotificationData
                    {
                        SequenceNumber = sequenceNumber,
                        PublishTime = publishTime
                    });
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            protected override MonitoredItem CreateMonitoredItem(MonitoredItemOptions? options)
            {
                return new ClientItem(this, ((ClientSession)Session).Application.LoggerFactory
                    .CreateLogger<ClientItem>());
            }
        }

        /// <inheritdoc/>
        private sealed class ClientItem : MonitoredItem
        {
            /// <inheritdoc/>
            public ClientItem(ClientSubscription subscription, ILogger logger)
                : base(subscription, logger)
            {
            }
        }

        /// <inheritdoc/>
        private sealed class MeterFactory : IMeterFactory
        {
            /// <inheritdoc/>
            public Meter Create(MeterOptions options)
            {
                return new Meter(options);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
            }
        }

        /// <summary>
        /// The delegate used to receive data change notifications.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <param name="stringTable"></param>
        public delegate void DataChangeNotificationHandler(Subscription subscription,
            DataChangeNotification notification, IReadOnlyList<string> stringTable);

        /// <summary>
        /// The delegate used to receive event notifications.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        /// <param name="stringTable"></param>
        public delegate void EventNotificationHandler(Subscription subscription,
            EventNotificationList notification, IReadOnlyList<string> stringTable);

        /// <summary>
        /// The delegate used to receive keep alive notifications.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notification"></param>
        public delegate void KeepAliveNotificationHandler(Subscription subscription,
            NotificationData notification);
    }
}
