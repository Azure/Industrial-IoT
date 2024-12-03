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
    /// <inheritdoc/>
    public class ClientOptionsBuilderBase<T> : IClientOptionsBuilder<T>,
        IOptionsBuilder<T>
        where T : ClientOptions, new()
    {
        /// <inheritdoc/>
        public T Options { get; set; } = new();

        /// <inheritdoc/>
        public IClientOptionsBuilder<T> WithReverseConnectPort(
            int reverseConnectPort)
        {
            Options = Options with { ReverseConnectPort = reverseConnectPort };
            return this;
        }

        /// <inheritdoc/>
        public IClientOptionsBuilder<T> WithMaxPooledSessions(
            int maxPooledSessions)
        {
            Options = Options with { MaxPooledSessions = maxPooledSessions };
            return this;
        }

        /// <inheritdoc/>
        public IClientOptionsBuilder<T> WithLingerTimeout(
            TimeSpan lingerTimeout)
        {
            Options = Options with { LingerTimeout = lingerTimeout };
            return this;
        }

        /// <inheritdoc/>
        public IClientOptionsBuilder<T> WithConnectStrategy(
            ResiliencePipeline connectStrategy)
        {
            Options = Options with { ConnectStrategy = connectStrategy };
            return this;
        }

        /// <inheritdoc/>
        public IClientOptionsBuilder<T> WithHostName(string hostName)
        {
            Options = Options with { HostName = hostName };
            return this;
        }

        /// <inheritdoc/>
        public IClientOptionsBuilder<T> UpdateApplicationFromExistingCert(
            bool updateApplicationFromExistingCert)
        {
            Options = Options with
            {
                UpdateApplicationFromExistingCert = updateApplicationFromExistingCert
            };
            return this;
        }
    }
}
