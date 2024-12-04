// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Opc.Ua;
using System.Threading;
using System;
using System.Threading.Tasks;
using Polly;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

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

/// <summary>
/// Session create options builder
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISessionCreateOptionsBuilder<T> : ISessionOptionsBuilder<T>
    where T : SessionOptions, new()
{
    /// <summary>
    /// Add available endpoints that should be validated
    /// </summary>
    /// <param name="availableEndpoints"></param>
    /// <returns></returns>
    ISessionCreateOptionsBuilder<T> WithAvailableEndpoints(
        EndpointDescriptionCollection availableEndpoints);

    /// <summary>
    /// Add a channel that should be used
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    ISessionCreateOptionsBuilder<T> WithChannel(
        ITransportChannel channel);

    /// <summary>
    /// Add a client certificate that should be used.
    /// </summary>
    /// <param name="clientCertificate"></param>
    /// <returns></returns>
    ISessionCreateOptionsBuilder<T> WithClientCertificate(
        X509Certificate2 clientCertificate);

    /// <summary>
    /// Add a connection that should be used
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    ISessionCreateOptionsBuilder<T> WithConnection(
        ITransportWaitingConnection connection);

    /// <summary>
    /// Add the discovery profile uris that should be validated
    /// </summary>
    /// <param name="discoveryProfileUris"></param>
    /// <returns></returns>
    ISessionCreateOptionsBuilder<T> WithDiscoveryProfileUris(
        StringCollection discoveryProfileUris);

    /// <summary>
    /// Set user identity
    /// </summary>
    /// <param name="identity"></param>
    /// <returns></returns>
    ISessionCreateOptionsBuilder<T> WithUser(
        IUserIdentity identity);

    /// <summary>
    /// Set reconnect strategy
    /// </summary>
    /// <param name="reconnectStrategy"></param>
    /// <returns></returns>
    ISessionCreateOptionsBuilder<T> WithReconnectStrategy(
        ResiliencePipeline reconnectStrategy);

    /// <summary>
    /// Build reconnect strategy
    /// </summary>
    /// <param name="reconnectStrategy"></param>
    /// <returns></returns>
    ISessionCreateOptionsBuilder<T> WithReconnectStrategy(
        Action<ResiliencePipelineBuilder> reconnectStrategy);
}

/// <summary>
/// Builder
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISessionOptionsBuilder<T>
    where T : SessionOptions, new()
{
    /// <summary>
    /// Set session name
    /// </summary>
    /// <param name="sessionName"></param>
    /// <returns></returns>
    ISessionOptionsBuilder<T> WithName(
        string sessionName);

    /// <summary>
    /// Set session timeout
    /// </summary>
    /// <param name="sessionTimeout"></param>
    /// <returns></returns>
    ISessionOptionsBuilder<T> WithTimeout(
        TimeSpan sessionTimeout);

    /// <summary>
    /// Set preferred locales
    /// </summary>
    /// <param name="preferredLocales"></param>
    /// <returns></returns>
    ISessionOptionsBuilder<T> WithPreferredLocales(
        IReadOnlyList<string> preferredLocales);

    /// <summary>
    /// Set keep alive interval
    /// </summary>
    /// <param name="keepAliveInterval"></param>
    /// <returns></returns>
    ISessionOptionsBuilder<T> WithKeepAliveInterval(
        TimeSpan keepAliveInterval);

    /// <summary>
    /// Set check domain
    /// </summary>
    /// <param name="checkDomain"></param>
    /// <returns></returns>
    ISessionOptionsBuilder<T> CheckDomain(
        bool checkDomain = true);

    /// <summary>
    /// Set disable complex type loading
    /// </summary>
    /// <param name="disableComplexTypeLoading"></param>
    /// <returns></returns>
    ISessionOptionsBuilder<T> DisableComplexTypeLoading(
        bool disableComplexTypeLoading = true);

    /// <summary>
    /// Set disable complex type preloading
    /// </summary>
    /// <param name="disableComplexTypePreloading"></param>
    /// <returns></returns>
    ISessionOptionsBuilder<T> DisableComplexTypePreloading(
        bool disableComplexTypePreloading = true);
}
