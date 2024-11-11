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
        public virtual Session Create(ITransportChannel channel,
            ApplicationConfiguration configuration, ConfiguredEndpoint endpoint,
            X509Certificate2? clientCertificate,
            EndpointDescriptionCollection? availableEndpoints,
            StringCollection? discoveryProfileUris)
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
            return await CreateAsyncCore(configuration, (ITransportWaitingConnection?)null, endpoint,
                updateBeforeConnect, checkDomain, sessionName, sessionTimeout,
                identity, preferredLocales, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ISession> CreateAsync(ApplicationConfiguration configuration,
            ITransportWaitingConnection connection, ConfiguredEndpoint endpoint,
            bool updateBeforeConnect, bool checkDomain, string sessionName,
            uint sessionTimeout, IUserIdentity identity, IList<string>? preferredLocales,
            CancellationToken ct)
        {
            return await CreateAsyncCore(configuration, connection, endpoint, updateBeforeConnect,
                checkDomain, sessionName, sessionTimeout, identity,
                preferredLocales, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async virtual Task<ISession> CreateAsync(ApplicationConfiguration configuration,
            ReverseConnectManager? reverseConnectManager, ConfiguredEndpoint endpoint,
            bool updateBeforeConnect, bool checkDomain, string sessionName,
            uint sessionTimeout, IUserIdentity userIdentity, IList<string>? preferredLocales,
            CancellationToken ct)
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
            if (sessionTemplate is not Session template)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionTemplate),
                    "The ISession provided is not of a supported type");
            }
            return await Session.RecreateAsync(template, null, connection,
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public virtual async Task<ISession> RecreateAsync(ISession sessionTemplate,
            ITransportChannel? transportChannel, CancellationToken ct)
        {
            if (sessionTemplate is not Session template)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionTemplate),
                    "The ISession provided is not of a supported type");
            }
            return await Session.RecreateAsync(template, transportChannel,
                null, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a secure channel to the specified endpoint.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="connection">The client endpoint for the reverse
        /// connect.</param>
        /// <param name="endpoint">A configured endpoint to connect to.</param>
        /// <param name="updateBeforeConnect">Update configuration based
        /// on server prior connect.</param>
        /// <param name="checkDomain">Check that the certificate specifies
        /// a valid domain (computer) name.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
        public static async Task<ITransportChannel> CreateChannelAsync(
            ApplicationConfiguration configuration, ITransportWaitingConnection? connection,
            ConfiguredEndpoint endpoint, bool updateBeforeConnect,
            bool checkDomain, CancellationToken ct = default)
        {
            endpoint.UpdateBeforeConnect = updateBeforeConnect;
            var endpointDescription = endpoint.Description;

            // create the endpoint configuration (use the application configuration
            // to provide default values).
            var endpointConfiguration = endpoint.Configuration;

            if (endpointConfiguration == null)
            {
                endpoint.Configuration = endpointConfiguration
                    = EndpointConfiguration.Create(configuration);
            }

            // create message context.
            var messageContext = configuration.CreateMessageContext(true);
            // update endpoint description using the discovery endpoint.
            if (endpoint.UpdateBeforeConnect && connection == null)
            {
                await endpoint.UpdateFromServerAsync(ct).ConfigureAwait(false);
                endpointDescription = endpoint.Description;
                endpointConfiguration = endpoint.Configuration;
            }

            // checks the domains in the certificate.
            if (checkDomain &&
                endpoint.Description.ServerCertificate?.Length > 0)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                configuration.CertificateValidator?.ValidateDomains(
                    new X509Certificate2(endpoint.Description.ServerCertificate),
                    endpoint);
#pragma warning restore CA2000 // Dispose objects before losing scope
                checkDomain = false;
            }

            X509Certificate2? clientCertificate = null;
            X509Certificate2Collection? clientCertificateChain = null;
            if (endpointDescription.SecurityPolicyUri != SecurityPolicies.None)
            {
                clientCertificate = await LoadCertificate(
                    configuration).ConfigureAwait(false);
                clientCertificateChain = await LoadCertificateChain(
                    configuration, clientCertificate).ConfigureAwait(false);
            }

            // initialize the channel which will be created with the server.
            if (connection != null)
            {
                return SessionChannel.CreateUaBinaryChannel(configuration,
                    connection, endpointDescription, endpointConfiguration,
                    clientCertificate, clientCertificateChain, messageContext);
            }

            return SessionChannel.Create(configuration, endpointDescription,
                 endpointConfiguration, clientCertificate, clientCertificateChain,
                 messageContext);
        }

        /// <summary>
        /// Creates a new communication session with a server using a
        /// reverse connection.
        /// </summary>
        /// <param name="configuration">The configuration for the client
        /// application.</param>
        /// <param name="connection">The client endpoint for the reverse
        /// connect.</param>
        /// <param name="endpoint">The endpoint for the server.</param>
        /// <param name="updateBeforeConnect">If set to <c>true</c> the
        /// discovery endpoint is used to update the endpoint description
        /// before connecting.</param>
        /// <param name="checkDomain">If set to <c>true</c> then the domain
        /// in the certificate must match the endpoint used.</param>
        /// <param name="sessionName">The name to assign to the session.
        /// </param>
        /// <param name="sessionTimeout">The timeout period for the session.
        /// </param>
        /// <param name="identity">The user identity to associate with the
        /// session.</param>
        /// <param name="preferredLocales">The preferred locales.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The new session object.</returns>
        private async Task<Session> CreateAsyncCore(ApplicationConfiguration configuration,
            ITransportWaitingConnection? connection, ConfiguredEndpoint endpoint,
            bool updateBeforeConnect, bool checkDomain, string sessionName,
            uint sessionTimeout, IUserIdentity identity, IList<string>? preferredLocales,
            CancellationToken ct = default)
        {
            // initialize the channel which will be created with the server.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var channel = await CreateChannelAsync(configuration,
                connection, endpoint, updateBeforeConnect, checkDomain,
                ct).ConfigureAwait(false);
#pragma warning restore CA2000 // Dispose objects before losing scope

            // create the session object.
            var session = this.Create(channel, configuration,
                endpoint, null, null, null);
            // create the session.
            try
            {
                await session.OpenAsync(sessionName, sessionTimeout, identity,
                    preferredLocales, checkDomain, ct).ConfigureAwait(false);
                return session;
            }
            catch (Exception)
            {
                session.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load certificate for connection.
        /// </summary>
        /// <param name="configuration"></param>
        /// <exception cref="ServiceResultException"></exception>
        private static async Task<X509Certificate2> LoadCertificate(
            ApplicationConfiguration configuration)
        {
            X509Certificate2 clientCertificate;
            var cert = configuration.SecurityConfiguration.ApplicationCertificate;
            if (cert == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                    "ApplicationCertificate must be specified.");
            }

            clientCertificate = await cert.Find(true).ConfigureAwait(false);

            if (clientCertificate == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                    "ApplicationCertificate cannot be found.");
            }
            return clientCertificate;
        }

        /// <summary>
        /// Load certificate chain for connection.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="clientCertificate"></param>
        private static async Task<X509Certificate2Collection?> LoadCertificateChain(
            ApplicationConfiguration configuration, X509Certificate2 clientCertificate)
        {
            X509Certificate2Collection? clientCertificateChain = null;
            // load certificate chain.
            if (configuration.SecurityConfiguration.SendCertificateChain)
            {
                clientCertificateChain = new X509Certificate2Collection(clientCertificate);
                var issuers = new List<CertificateIdentifier>();
                await configuration.CertificateValidator.GetIssuers(
                    clientCertificate, issuers).ConfigureAwait(false);

                for (var i = 0; i < issuers.Count; i++)
                {
                    clientCertificateChain.Add(issuers[i].Certificate);
                }
            }
            return clientCertificateChain;
        }
    }
}
