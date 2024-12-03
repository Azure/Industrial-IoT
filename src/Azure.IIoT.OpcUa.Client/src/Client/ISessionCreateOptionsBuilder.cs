// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua;
    using Polly;
    using System.Security.Cryptography.X509Certificates;

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
    }
}
