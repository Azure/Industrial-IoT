// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session builder base
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="S"></typeparam>
    /// <typeparam name="C"></typeparam>
    /// <typeparam name="CBuilder"></typeparam>
    /// <remarks>
    /// Create builder
    /// </remarks>
    /// <param name="application"></param>
    /// <param name="pooledSessionBuilder"></param>
    public class SessionBuilderBase<T, S, C, CBuilder>(ClientApplicationBase application,
        IPooledSessionBuilder<T, S> pooledSessionBuilder) : ISessionBuilder<T, S, C>,
        IOptionsBuilder<EndpointDescription>
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
        where C : SessionCreateOptions, new()
        where CBuilder : ISessionCreateOptionsBuilder<C>, new()
    {
        /// <inheritdoc/>
        public IPooledSessionBuilder<T, S> FromPool { get; } = pooledSessionBuilder;

        /// <inheritdoc/>
        public ICertificates Certificates => application;

        /// <inheritdoc/>
        public EndpointDescription Options => ((IOptionsBuilder<T>)FromPool).Options.Endpoint;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public ISessionBuilder<T, S, C> ConnectTo(
            string endpointUrl)
        {
            Options.EndpointUrl = endpointUrl;
            return this;
        }

        /// <inheritdoc/>
        public ISessionBuilder<T, S, C> WithSecurityMode(
            MessageSecurityMode securityMode)
        {
            Options.SecurityMode = securityMode;
            return this;
        }

        /// <inheritdoc/>
        public ISessionBuilder<T, S, C> WithSecurityPolicy(
            string securityPolicyUri)
        {
            Options.SecurityPolicyUri = securityPolicyUri;
            return this;
        }

        /// <inheritdoc/>
        public ISessionBuilder<T, S, C> WithServerCertificate(
            byte[] serverCertificate)
        {
            Options.ServerCertificate = serverCertificate;
            return this;
        }

        /// <inheritdoc/>
        public ISessionBuilder<T, S, C> WithTransportProfileUri(
            string transportProfileUri)
        {
            Options.TransportProfileUri = transportProfileUri;
            return this;
        }

        /// <inheritdoc/>
        public IUnpooledSessionBuilder<C> WithOption(
            Action<ISessionCreateOptionsBuilder<C>> configure)
        {
            configure(_sessionCreateOptionsBuilder);
            return this;
        }

        /// <inheritdoc/>
        public IUnpooledSessionBuilder<C> UseReverseConnect(
            bool useReverseConnect = true)
        {
            _useReverseConnect = useReverseConnect;
            return this;
        }

        /// <inheritdoc/>
        public ValueTask<ISession> CreateAsync(CancellationToken ct = default)
        {
            return application.ConnectAsync(Options,
                ((IOptionsBuilder<C>)_sessionCreateOptionsBuilder).Options,
                _useReverseConnect, ct);
        }

        /// <inheritdoc/>
        public ValueTask<ServiceResult> TestAsync(CancellationToken ct = default)
        {
            return application.TestAsync(Options,
               // ((IOptionsBuilder<C>)_sessionCreateOptionsBuilder).Options,
                _useReverseConnect, ct);
        }

        /// <summary>
        /// Called when disposed
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            application.Dispose();
        }

        private readonly CBuilder _sessionCreateOptionsBuilder = new();
        private bool _useReverseConnect;
    }

    /// <summary>
    /// Builder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="S"></typeparam>
    /// <typeparam name="SBuilder"></typeparam>
    /// <param name="application"></param>
    public class PooledSessionBuilderBase<T, S, SBuilder>(IConnectionManager application) :
        IPooledSessionBuilder<T, S>, IOptionsBuilder<T>
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
        where SBuilder: ISessionOptionsBuilder<S>, new()
    {
        /// <inheritdoc/>
        public T Options { get; set; } = new();

        /// <inheritdoc/>
        public ValueTask<PooledSession> CreateAsync(CancellationToken ct = default)
        {
            Debug.Assert(Options.Endpoint.EndpointUrl != null);
            return application.GetOrConnectAsync(Options, ct);
        }

        /// <inheritdoc/>
        public IPooledSessionBuilder<T, S> WithUser(
            IUserIdentity user)
        {
            Options = Options with { User = user };
            return this;
        }

        /// <inheritdoc/>
        public IPooledSessionBuilder<T, S> WithOption(
            Action<ISessionOptionsBuilder<S>> configure)
        {
            configure(_sessionOptionsBuilder);
            Options = Options with
            {
                SessionOptions = ((IOptionsBuilder<S>)_sessionOptionsBuilder).Options
            };
            return this;
        }

        /// <inheritdoc/>
        public IPooledSessionBuilder<T, S> UseReverseConnect(
            bool useReverseConnect)
        {
            Options = Options with
            {
                UseReverseConnect = useReverseConnect
            };
            return this;
        }

        private readonly SBuilder _sessionOptionsBuilder = new();
    }
}
