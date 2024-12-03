// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua;
    using System.Threading;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Session builder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="S"></typeparam>
    /// <typeparam name="C"></typeparam>
    public interface ISessionBuilder<T, S, C> :
        IUnpooledSessionBuilder<C>, IClient, IDisposable
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
        where C : SessionCreateOptions, new()
    {
        /// <summary>
        /// With the endpoint url
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <returns></returns>
        ISessionBuilder<T, S, C> ConnectTo(
            string endpointUrl);

        /// <summary>
        /// And the security mode
        /// </summary>
        /// <param name="securityMode"></param>
        /// <returns></returns>
        ISessionBuilder<T, S, C> WithSecurityMode(
            MessageSecurityMode securityMode);

        /// <summary>
        /// And security policy
        /// </summary>
        /// <param name="securityPolicyUri"></param>
        /// <returns></returns>
        ISessionBuilder<T, S, C> WithSecurityPolicy(
            string securityPolicyUri);

        /// <summary>
        /// And the server certificate
        /// </summary>
        /// <param name="serverCertificate"></param>
        /// <returns></returns>
        ISessionBuilder<T, S, C> WithServerCertificate(
            byte[] serverCertificate);

        /// <summary>
        /// Using the provided transport
        /// </summary>
        /// <param name="transportProfileUri"></param>
        /// <returns></returns>
        ISessionBuilder<T, S, C> WithTransportProfileUri(
            string transportProfileUri);

        /// <summary>
        /// Pooled session
        /// </summary>
        IPooledSessionBuilder<T, S> FromPool { get; }
    }

    /// <summary>
    /// Session builder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IUnpooledSessionBuilder<T>
        where T : SessionCreateOptions, new()
    {
        /// <summary>
        /// Use reverse connect to connect
        /// </summary>
        /// <param name="useReverseConnect"></param>
        /// <returns></returns>
        IUnpooledSessionBuilder<T> UseReverseConnect(
            bool useReverseConnect = true);

        /// <summary>
        /// The session create options to use.
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        IUnpooledSessionBuilder<T> WithOption(
            Action<ISessionCreateOptionsBuilder<T>> configure);

        /// <summary>
        /// Create session
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISession> CreateAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Test connectivity
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ServiceResult> TestAsync(
            CancellationToken ct = default);
    }

    /// <summary>
    /// Pooled session builder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="S"></typeparam>
    public interface IPooledSessionBuilder<T, S>
        where T : PooledSessionOptions, new()
        where S : SessionOptions, new()
    {
        /// <summary>
        /// Connect the pooled client
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<PooledSession> CreateAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Use reverse connect to connect
        /// </summary>
        /// <param name="useReverseConnect"></param>
        /// <returns></returns>
        IPooledSessionBuilder<T, S> UseReverseConnect(
            bool useReverseConnect = true);

        /// <summary>
        /// Conbfigure the session options
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        IPooledSessionBuilder<T, S> WithOption(
            Action<ISessionOptionsBuilder<S>> configure);

        /// <summary>
        /// User identity to use when connecting
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        IPooledSessionBuilder<T, S> WithUser(
            IUserIdentity user);
    }
}
