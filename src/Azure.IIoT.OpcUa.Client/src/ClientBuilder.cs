// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// This is a simple client api that exposes the ability of the sdk using a
    /// fluent API that can be used like int he following example:
    /// <code>
    /// var builder = new ClientBuilder();
    /// var client = builder.NewClient
    ///     .WithName("Test")
    ///     .WithUri("uri")
    ///     .WithProductUri("Pro")
    ///     .WithTransportQuota(o => o.SetMaxBufferSize(100))
    ///     .WithConfiguration(c => c.AddSecurityConfiguration("a", "b"))
    ///     .WithOption(o => o.WithHostName("host"))
    ///     .WithOption(o => o.UpdateApplicationFromExistingCert())
    ///     .WithOption(o => o.WithMaxPooledSessions(10))
    ///     .Build();
    ///
    /// var ownCertificate = await client.Certificates.GetOwnCertificateAsync();
    ///
    /// using var session = await client
    ///     .ConnectTo("endpointUrl")
    ///     .WithSecurityMode(MessageSecurityMode.SignAndEncrypt)
    ///     .WithSecurityPolicy(SecurityPolicies.None)
    ///     .WithServerCertificate([])
    ///     .FromPool
    ///         .WithOption(o => o.WithTimeout(TimeSpan.FromMicroseconds(1000)))
    ///         .WithOption(o => o.WithKeepAliveInterval(TimeSpan.FromSeconds(30)))
    ///         .CreateAsync().ConfigureAwait(false);
    ///
    /// await session.Session.CallAsync(null, null).ConfigureAwait(false);
    /// using var direct = await client
    ///     .ConnectTo("endpointur")
    ///     .UseReverseConnect()
    ///     .WithOption(o => o.WithUser(new UserIdentity()))
    ///     .CreateAsync()
    ///     .ConfigureAwait(false);
    ///
    /// await direct.CallAsync(null, null).ConfigureAwait(false);
    /// </code>
    /// </summary>
    /// <param name="services"></param>
    public class ClientBuilder(IServiceCollection? services = null) :
        ClientBuilderBase<PooledSessionOptions, SessionOptions, SessionCreateOptions,
            ClientOptions, ClientOptionsBuilderBase<ClientOptions>>(services)
    {
        /// <inheritdoc/>
        protected override ISessionBuilder<PooledSessionOptions, SessionOptions, SessionCreateOptions> Build(
            ServiceProvider provider, ClientOptions options,
            ApplicationConfiguration applicationConfiguration, IObservability observability)
        {
            var application = new ClientApplication(applicationConfiguration, options, observability);
            return new ClientSessionBuilder(provider, application);
        }

        /// <summary>
        /// Session builder
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="application"></param>
        internal class ClientSessionBuilder(ServiceProvider provider, ClientApplicationBase application) :
            SessionBuilderBase<PooledSessionOptions, SessionOptions, SessionCreateOptions,
                SessionCreateOptionsBuilder<SessionCreateOptions>>(application,
                new PooledSessionBuilderBase<PooledSessionOptions, SessionOptions,
                    SessionOptionsBuilderBase<SessionOptions>>(application))
        {
            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    provider.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Client application built by the client builder and input into the session builder.
        /// It encapsulates the state of the application.
        /// </summary>
        /// <remarks>
        /// Create client application
        /// </remarks>
        /// <remarks>
        /// Create session builder
        /// </remarks>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        /// <param name="observability"></param>
        internal class ClientApplication(ApplicationConfiguration configuration, ClientOptions options,
            IObservability observability) : ClientApplicationBase(configuration, options, observability)
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

            /// <inheritdoc/>
            protected override Session CreateSession(ApplicationConfiguration configuration,
                ConfiguredEndpoint endpoint, SessionCreateOptions options, IObservability observability)
            {
                return new ClientSession(this, configuration, endpoint, options, observability);
            }

            /// <inheritdoc/>
            public override string GetPassword(CertificateIdentifier certificateIdentifier)
            {
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        internal sealed class ClientSession : Session
        {
            /// <inheritdoc/>
            public ClientApplication Application { get; }

            /// <inheritdoc/>
            internal ClientSession(ClientApplication application, ApplicationConfiguration configuration,
                ConfiguredEndpoint endpoint, SessionCreateOptions options, IObservability observability)
                : base(configuration, endpoint, options, observability, application.ReverseConnectManager)
            {
                Application = application;
            }

            /// <inheritdoc/>
            public override IManagedSubscription CreateSubscription(IObservability observability,
                IOptionsMonitor<SubscriptionOptions> options, IMessageAckQueue queue)
            {
                return new ClientSubscription(this, queue, options, observability);
            }
        }

        /// <inheritdoc/>
        internal sealed class ClientSubscription : Subscription
        {
            /// <inheritdoc/>
            internal ClientSubscription(ClientSession session, IMessageAckQueue completion,
                IOptionsMonitor<SubscriptionOptions> options, IObservability observability) :
                base(session, completion, options, observability)
            {
                _session = session;
            }

            /// <inheritdoc/>
            protected override ValueTask OnDataChangeNotificationAsync(uint sequenceNumber,
                DateTime publishTime, DataChangeNotification notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                notification.PublishTime = publishTime;
                notification.SequenceNumber = sequenceNumber;
                _session.Application.DataChangeCallback?.Invoke(this, notification, stringTable);
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            protected override ValueTask OnEventDataNotificationAsync(uint sequenceNumber,
                DateTime publishTime, EventNotificationList notification,
                PublishState publishStateMask, IReadOnlyList<string> stringTable)
            {
                notification.PublishTime = publishTime;
                notification.SequenceNumber = sequenceNumber;
                _session.Application.EventCallback?.Invoke(this, notification, stringTable);
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            protected override ValueTask OnKeepAliveNotificationAsync(uint sequenceNumber,
                DateTime publishTime, PublishState publishStateMask)
            {
                _session.Application.KeepAliveCallback?.Invoke(this, new NotificationData
                {
                    SequenceNumber = sequenceNumber,
                    PublishTime = publishTime
                });
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            protected override MonitoredItem CreateMonitoredItem(IObservability observability,
                IOptionsMonitor<MonitoredItemOptions> options)
            {
                return new ClientItem(this, options,
                    observability.LoggerFactory.CreateLogger<ClientItem>());
            }

            private new readonly ClientSession _session;
        }

        /// <inheritdoc/>
        internal sealed class ClientItem : MonitoredItem
        {
            /// <inheritdoc/>
            internal ClientItem(ClientSubscription subscription,
                IOptionsMonitor<MonitoredItemOptions> options, ILogger logger)
                : base(subscription, options, logger)
            {
            }
        }
    }

    /// <summary>
    /// The delegate used to receive data change notifications.
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="notification"></param>
    /// <param name="stringTable"></param>
    public delegate void DataChangeNotificationHandler(ISubscription subscription,
        DataChangeNotification notification, IReadOnlyList<string> stringTable);

    /// <summary>
    /// The delegate used to receive event notifications.
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="notification"></param>
    /// <param name="stringTable"></param>
    public delegate void EventNotificationHandler(ISubscription subscription,
        EventNotificationList notification, IReadOnlyList<string> stringTable);

    /// <summary>
    /// The delegate used to receive keep alive notifications.
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="notification"></param>
    public delegate void KeepAliveNotificationHandler(ISubscription subscription,
        NotificationData notification);
}
