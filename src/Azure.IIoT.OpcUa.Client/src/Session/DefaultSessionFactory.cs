/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Object that creates instances of an Session objects.
    /// </summary>
    public class DefaultSessionFactory : ISessionFactory, ISessionInstantiator
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Default instance
        /// </summary>
        /// <param name="loggerFactory"></param>
        public DefaultSessionFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc/>
        public virtual Session Create(ITransportChannel channel, ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint, X509Certificate2? clientCertificate,
            EndpointDescriptionCollection? availableEndpoints, StringCollection? discoveryProfileUris)
        {
            return new Session(channel, configuration, endpoint, clientCertificate,
                _loggerFactory, availableEndpoints, discoveryProfileUris);
        }

        /// <inheritdoc/>
        public async virtual Task<ISession> CreateAsync(ApplicationConfiguration configuration,
            ConfiguredEndpoint endpoint, bool updateBeforeConnect, bool checkDomain,
            string sessionName, uint sessionTimeout, IUserIdentity identity,
            IList<string>? preferredLocales, CancellationToken ct)
        {
            return await Session.Create(this, configuration, (ITransportWaitingConnection?)null,
                endpoint, updateBeforeConnect, checkDomain, sessionName, sessionTimeout, identity,
                preferredLocales, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async virtual Task<ISession> CreateAsync(ApplicationConfiguration configuration,
            ITransportWaitingConnection connection, ConfiguredEndpoint endpoint,
            bool updateBeforeConnect, bool checkDomain, string sessionName, uint sessionTimeout,
            IUserIdentity identity, IList<string>? preferredLocales, CancellationToken ct)
        {
            return await Session.Create(this, configuration, connection, endpoint,
                updateBeforeConnect, checkDomain, sessionName, sessionTimeout,
                identity, preferredLocales, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async virtual Task<ISession> CreateAsync(ApplicationConfiguration configuration,
            ReverseConnectManager? reverseConnectManager, ConfiguredEndpoint endpoint,
            bool updateBeforeConnect, bool checkDomain, string sessionName, uint sessionTimeout,
            IUserIdentity userIdentity, IList<string>? preferredLocales, CancellationToken ct)
        {
            if (reverseConnectManager == null)
            {
                return await CreateAsync(configuration, endpoint, updateBeforeConnect,
                    checkDomain, sessionName, sessionTimeout, userIdentity,
                    preferredLocales, ct).ConfigureAwait(false);
            }
            ITransportWaitingConnection? connection;
            do
            {
                connection = await reverseConnectManager.WaitForConnection(endpoint.EndpointUrl,
                    endpoint.ReverseConnect?.ServerUri, ct).ConfigureAwait(false);
                if (updateBeforeConnect)
                {
                    await endpoint.UpdateFromServerAsync(endpoint.EndpointUrl, connection,
                        endpoint.Description.SecurityMode, endpoint.Description.SecurityPolicyUri,
                        ct).ConfigureAwait(false);
                    updateBeforeConnect = false;
                    connection = null;
                }
            }
            while (connection == null);

            return await CreateAsync(configuration, connection, endpoint, false, checkDomain,
                sessionName, sessionTimeout, userIdentity, preferredLocales, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public virtual async Task<ISession> RecreateAsync(ISession sessionTemplate,
            ITransportWaitingConnection connection, CancellationToken ct)
        {
            if (!(sessionTemplate is Session template))
            {
                throw new ArgumentOutOfRangeException(nameof(sessionTemplate),
                    "The ISession provided is not of a supported type");
            }
            return await Session.RecreateAsync(template, connection, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public virtual async Task<ISession> RecreateAsync(ISession sessionTemplate,
            ITransportChannel? transportChannel, CancellationToken ct)
        {
            if (!(sessionTemplate is Session template))
            {
                throw new ArgumentOutOfRangeException(nameof(sessionTemplate),
                    "The ISession provided is not of a supported type");
            }
            return await Session.RecreateAsync(template, transportChannel, ct).ConfigureAwait(false);
        }
    }
}
