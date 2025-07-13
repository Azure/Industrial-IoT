// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly.Exceptions;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Console host for servers
    /// </summary>
    public sealed class ServerConsoleHost : IServerHost
    {
        /// <inheritdoc/>
        public X509Certificate2 Certificate { get; private set; }
        /// <inheritdoc/>
        public string PkiRootPath { get; set; }
        /// <inheritdoc/>
        public bool AutoAccept { get; set; }
        /// <inheritdoc/>
        public string HostName { get; set; }
        /// <inheritdoc/>
        public List<string> AlternativeHosts { get; set; }
        /// <inheritdoc/>
        public string UriPath { get; set; }
        /// <inheritdoc/>
        public string CertStoreType { get; set; }

        /// <summary>
        /// Get access to the server
        /// </summary>
        public ITestServer TestServer => _server as ITestServer;

        /// <summary>
        /// Create server console host
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        public ServerConsoleHost(IServerFactory factory, ILogger<ServerConsoleHost> logger)
        {
            _instance = Guid.NewGuid().ToString();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public async Task StopAsync()
        {
            if (_server != null)
            {
                await _lock.WaitAsync().ConfigureAwait(false);
                try
                {
#pragma warning disable CA1508 // Avoid dead conditional code
                    if (_server != null)
                    {
                        _logger.StoppingServer(this);
                        try
                        {
                            _server.Stop();
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception se)
                        {
                            _logger.ServerStopError(se, this);
                        }
                        _server.Dispose();
                        _logger.ServerStopped(this);
                    }
#pragma warning restore CA1508 // Avoid dead conditional code
                }
                catch (Exception ce)
                {
                    _logger.StoppingError(ce, this);
                }
                finally
                {
                    _server = null;
                    Certificate = null;
                    _lock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task AddReverseConnectionAsync(Uri client, int maxSessionCount)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_server is ReverseConnectServer server)
                {
                    server.AddReverseConnection(client, maxSessionCount: maxSessionCount);
                }
            }
            catch (Exception ex)
            {
                _logger.AddReverseConnectionError(ex, this);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RemoveReverseConnectionAsync(Uri client)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_server is ReverseConnectServer server)
                {
                    server.RemoveReverseConnection(client);
                }
            }
            catch (Exception ex)
            {
                _logger.RemoveReverseConnectionError(ex, this);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(IEnumerable<int> ports)
        {
            if (_server == null)
            {
                await _lock.WaitAsync().ConfigureAwait(false);
                try
                {
#pragma warning disable CA1508 // Avoid dead conditional code
                    if (_server == null)
                    {
                        await StartServerInternalAsync(ports).ConfigureAwait(false);
                        _ports = ports.ToArray();
                        return;
                    }
#pragma warning restore CA1508 // Avoid dead conditional code
                }
                catch (Exception ex)
                {
                    _logger.StartingError(ex, this);
                    _server?.Dispose();
                    _server = null;
                    throw;
                }
                finally
                {
                    _lock.Release();
                }
            }
            throw new InvalidOperationException($"Server {this} already started");
        }

        /// <inheritdoc/>
        public async Task RestartAsync(Func<Task> predicate)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_server != null)
                {
                    _server.Stop();
                    _server.Dispose();

                    if (predicate != null)
                    {
                        await predicate().ConfigureAwait(false);
                    }

                    _logger.Restarting(this);
                    Debug.Assert(_ports != null);

                    await StartServerInternalAsync(_ports).ConfigureAwait(false);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            StopAsync().WaitAsync(TimeSpan.FromMinutes(1)).GetAwaiter().GetResult();
            _lock.Dispose();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return _instance;
        }

        /// <summary>
        /// Start server
        /// </summary>
        /// <param name="ports"></param>
        /// <returns></returns>
        /// <exception cref="InvalidConfigurationException"></exception>
        private async Task StartServerInternalAsync(IEnumerable<int> ports)
        {
            ApplicationInstance.MessageDlg = new DummyDialog();

            var config = _factory.CreateServer(ports, PkiRootPath, out _server,
                listenHostName: HostName,
                alternativeAddresses: AlternativeHosts,
                path: UriPath,
                certStoreType: CertStoreType,
                configure: configuration => configuration.DiagnosticsEnabled = true);
            _logger.ServerCreated(this);

            config.SecurityConfiguration.AutoAcceptUntrustedCertificates = AutoAccept;
            config = ApplicationInstance.FixupAppConfig(config);

            _logger.ValidatingConfig(this);
            await config.Validate(config.ApplicationType).ConfigureAwait(false);

            _logger.InitializingCertValidation(this);
            var application = new ApplicationInstance(config);

            // check the application certificate.
            var hasAppCertificate = await application.CheckApplicationInstanceCertificates(
                silent: true).ConfigureAwait(false);
            if (!hasAppCertificate)
            {
                _logger.CertValidationError(this);
                throw new InvalidConfigurationException("Application instance certificate invalid!");
            }

            config.CertificateValidator.CertificateValidation += (v, e) =>
            {
                if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
                {
                    e.Accept = AutoAccept;
                    _logger.CertificateAction(this,
                        e.Accept ? "Accepted" : "Rejected", e.Certificate.Subject);
                }
            };

            await config.CertificateValidator.Update(config).ConfigureAwait(false);

            // Set Certificate
            try
            {
                // just take the public key
                Certificate = X509CertificateLoader.LoadCertificate(
                    config.SecurityConfiguration.ApplicationCertificate.Certificate.RawData);
            }
            catch
            {
                Certificate = config.SecurityConfiguration.ApplicationCertificate.Certificate;
            }

            _logger.StartingServer();
            // start the server.
            await application.Start(_server).ConfigureAwait(false);

            foreach (var ep in config.ServerConfiguration.BaseAddresses)
            {
                _logger.ServerEndpoint(this, ep);
            }

            _logger.ServerStarted(this);
        }

        /// <inheritdoc/>
        private sealed class DummyDialog : IApplicationMessageDlg
        {
            /// <inheritdoc/>
            public override void Message(string text, bool ask) { }
            /// <inheritdoc/>
            public override Task<bool> ShowAsync()
            {
                return Task.FromResult(true);
            }
        }

        private readonly string _instance;
        private readonly ILogger _logger;
        private readonly IServerFactory _factory;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private ServerBase _server;
        private int[] _ports;
    }

    /// <summary>
    /// Source-generated logging definitions for ServerConsoleHost
    /// </summary>
    internal static partial class ServerConsoleHostLogging
    {
        private const int EventClass = 100;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Information,
            Message = "Stopping server {Instance}.")]
        public static partial void StoppingServer(this ILogger logger, object instance);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Error,
            Message = "Server {Instance} not cleanly stopped.")]
        public static partial void ServerStopError(this ILogger logger, Exception ex, object instance);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Information,
            Message = "Server {Instance} stopped.")]
        public static partial void ServerStopped(this ILogger logger, object instance);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Error,
            Message = "Stopping server {Instance} caused exception.")]
        public static partial void StoppingError(this ILogger logger, Exception ex, object instance);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Error,
            Message = "Adding reverse connection in server {Instance} failed.")]
        public static partial void AddReverseConnectionError(this ILogger logger, Exception ex, object instance);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Error,
            Message = "Remove reverse connection in server {Instance} failed.")]
        public static partial void RemoveReverseConnectionError(this ILogger logger, Exception ex, object instance);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Error,
            Message = "Starting server {Instance} caused exception.")]
        public static partial void StartingError(this ILogger logger, Exception ex, object instance);

        [LoggerMessage(EventId = EventClass + 8, Level = LogLevel.Information,
            Message = "Restarting server {Instance}...")]
        public static partial void Restarting(this ILogger logger, object instance);

        [LoggerMessage(EventId = EventClass + 9, Level = LogLevel.Information,
            Message = "Server {Instance} created...")]
        public static partial void ServerCreated(this ILogger logger, object instance);

        [LoggerMessage(EventId = EventClass + 10, Level = LogLevel.Information,
            Message = "Server {Instance} - Validate configuration...")]
        public static partial void ValidatingConfig(this ILogger logger, object instance);

        [LoggerMessage(EventId = EventClass + 11, Level = LogLevel.Information,
            Message = "Server {Instance} - Initialize certificate validation...")]
        public static partial void InitializingCertValidation(this ILogger logger, object instance);

        [LoggerMessage(EventId = EventClass + 12, Level = LogLevel.Error,
            Message = "Server {Instance} - Failed validating own certificate!")]
        public static partial void CertValidationError(this ILogger logger, object instance);

        [LoggerMessage(EventId = EventClass + 13, Level = LogLevel.Information,
            Message = "Server {Instance} - {Action} Certificate {Subject}")]
        public static partial void CertificateAction(this ILogger logger, object instance, string action, string subject);

        [LoggerMessage(EventId = EventClass + 14, Level = LogLevel.Information,
            Message = "Starting server ...")]
        public static partial void StartingServer(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 15, Level = LogLevel.Information,
            Message = "Server {Instance} - Listening on {Endpoint}")]
        public static partial void ServerEndpoint(this ILogger logger, object instance, string endpoint);

        [LoggerMessage(EventId = EventClass + 16, Level = LogLevel.Information,
            Message = "Server {Instance} started.")]
        public static partial void ServerStarted(this ILogger logger, object instance);
    }
}
