// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Session options
    /// </summary>
    public record class SessionOptions
    {
        /// <summary>
        /// The name of the session. The name is displayed
        /// on the server to help administrators identify the
        /// client. The name is mandatory.
        /// </summary>
        public required string SessionName { get; init; }

        /// <summary>
        /// Desired Session timeout after which the session
        /// is garbage collected on the server without any
        /// activity. This setting can be revised by the
        /// server and the actual timeout is exposed by the
        /// <see cref="ISession"/>.
        /// </summary>
        public TimeSpan? SessionTimeout { get; init; }

        /// <summary>
        /// The user identity to use on the session.
        /// The default is anonymous user.
        /// </summary>
        public IUserIdentity? Identity { get; init; }

        /// <summary>
        /// Preferred locales to use on this session. The default
        /// locales used are en-Us.
        /// </summary>
        public IReadOnlyList<string>? PreferredLocales { get; init; }

        /// <summary>
        /// Gets or Sets how frequently the server is pinged to
        /// see if communication is still working. This interval
        /// controls how much time elaspes before a communication
        /// error is detected. The session will send read request
        /// when the keep alive interval expires. The keep alive
        /// timer is reset any time a successful response is
        /// returned (from any service, including publish and the
        /// keep alive read operation)
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; init; }

        /// <summary>
        /// Check domain of the certificate against the endpoint
        /// of the server during session creation.
        /// </summary>
        public bool CheckDomain { get; init; }

        /// <summary>
        /// Available endpoints the server presented during initial
        /// discovery. Leave blank to disable any validation during
        /// session creation against the available endpoints found
        /// in discovery.
        /// </summary>
        public EndpointDescriptionCollection? AvailableEndpoints { get; init; }

        /// <summary>
        /// Discovery profile uris that were returned in the initial
        /// discovery sequence.
        /// </summary>
        public StringCollection? DiscoveryProfileUris { get; init; }

        /// <summary>
        /// No complex type loading ever. This will effectively
        /// disable any use of the GetComplexTypeSystemAsync() API.
        /// </summary>
        public bool DisableComplexTypeLoading { get; init; }

        /// <summary>
        /// Disable complex type preloading when session is created.
        /// The complex type can then be lazily loaded using the
        /// GetComplexTypeSystemAsync() API.
        /// </summary>
        public bool DisableComplexTypePreloading { get; init; }

        /// <summary>
        /// Client certificate to use. If not provided or not valid
        /// the session will load the certificate from the own
        /// store.
        /// </summary>
        public X509Certificate2? ClientCertificate { get; init; }

        /// <summary>
        /// An existing channel to use. This can be an open channel
        /// that was used during discovery. The session will create
        /// a new channel if the channel is not workable.
        /// </summary>
        public ITransportChannel? Channel { get; init; }

        /// <summary>
        /// Connection to use. One can pass an existing reverse
        /// connection, the session however will create one if the
        /// connection is closed or unusable.
        /// </summary>
        public ITransportWaitingConnection? Connection { get; init; }

        /// <summary>
        /// Reconnect policy to use. The resiliency pipelines will
        /// be used to reconnect the session when the connection
        /// is determined lost. Use rate limiting to limit the
        /// number of reconnects across the entire client.
        /// </summary>
        public ResiliencePipeline? ReconnectStrategy { get; init; }
    }
}
