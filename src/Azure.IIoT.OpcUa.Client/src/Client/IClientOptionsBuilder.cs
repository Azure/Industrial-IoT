// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Polly;
    using System;

    /// <summary>
    /// Build client options
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IClientOptionsBuilder<T>
        where T : ClientOptions, new()
    {
        /// <summary>
        /// Update application configuration from existing certificate.
        /// </summary>
        /// <param name="updateApplicationFromExistingCert"></param>
        /// <returns></returns>
        IClientOptionsBuilder<T> UpdateApplicationFromExistingCert(
            bool updateApplicationFromExistingCert = true);

        /// <summary>
        /// Use the connection strategy to connect to the server.
        /// </summary>
        /// <param name="connectStrategy"></param>
        /// <returns></returns>
        IClientOptionsBuilder<T> WithConnectStrategy(
            ResiliencePipeline connectStrategy);

        /// <summary>
        /// Use this host name instead of the one returned from the host.
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        IClientOptionsBuilder<T> WithHostName(
            string hostName);

        /// <summary>
        /// Use this timeout for the session pools
        /// </summary>
        /// <param name="lingerTimeout"></param>
        /// <returns></returns>
        IClientOptionsBuilder<T> WithLingerTimeout(
            TimeSpan lingerTimeout);

        /// <summary>
        /// Use the max pooled sessions specified
        /// </summary>
        /// <param name="maxPooledSessions"></param>
        /// <returns></returns>
        IClientOptionsBuilder<T> WithMaxPooledSessions(
            int maxPooledSessions);

        /// <summary>
        /// Use the reverse connect port
        /// </summary>
        /// <param name="reverseConnectPort"></param>
        /// <returns></returns>
        IClientOptionsBuilder<T> WithReverseConnectPort(
            int reverseConnectPort);
    }
}
