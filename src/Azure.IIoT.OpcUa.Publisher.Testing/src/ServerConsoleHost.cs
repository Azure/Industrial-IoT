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

        /// <summary>
        /// Create server console host
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        public ServerConsoleHost(IServerFactory factory, ILogger<ServerConsoleHost> logger)
        {
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
                        _logger.LogInformation("Stopping server.");
                        try
                        {
                            _server.Stop();
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception se)
                        {
                            _logger.LogError(se, "Server not cleanly stopped.");
                        }
                        _server.Dispose();
                    }
#pragma warning restore CA1508 // Avoid dead conditional code
                    _logger.LogInformation("Server stopped.");
                }
                catch (Exception ce)
                {
                    _logger.LogError(ce, "Stopping server caused exception.");
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
                _logger.LogError(ex, "Adding reverse connection failed.");
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
                _logger.LogError(ex, "Remove reverse connection failed.");
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
                        await StartServerInternalAsync(ports, PkiRootPath).ConfigureAwait(false);
                        return;
                    }
#pragma warning restore CA1508 // Avoid dead conditional code
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Starting server caused exception.");
                    _server?.Dispose();
                    _server = null;
                    throw;
                }
                finally
                {
                    _lock.Release();
                }
            }
            throw new InvalidOperationException("Already started");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
            _lock.Dispose();
        }

        /// <summary>
        /// Start server
        /// </summary>
        /// <param name="ports"></param>
        /// <param name="pkiRootPath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidConfigurationException"></exception>
        private async Task StartServerInternalAsync(IEnumerable<int> ports, string pkiRootPath)
        {
            ApplicationInstance.MessageDlg = new DummyDialog();

            var config = _factory.CreateServer(ports, pkiRootPath, out _server);
            _logger.LogInformation("Server created...");

            config.SecurityConfiguration.AutoAcceptUntrustedCertificates = AutoAccept;
            config = ApplicationInstance.FixupAppConfig(config);

            _logger.LogInformation("Validate configuration...");
            await config.Validate(config.ApplicationType).ConfigureAwait(false);

            _logger.LogInformation("Initialize certificate validation...");
            var application = new ApplicationInstance(config);

            // check the application certificate.
            var hasAppCertificate = await application.CheckApplicationInstanceCertificate(
                silent: true, CertificateFactory.DefaultKeySize).ConfigureAwait(false);
            if (!hasAppCertificate)
            {
                _logger.LogError("Failed validating own certificate!");
                throw new InvalidConfigurationException("Application instance certificate invalid!");
            }

            config.CertificateValidator.CertificateValidation += (v, e) =>
            {
                if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
                {
                    e.Accept = AutoAccept;
                    _logger.LogInformation("{Action} Certificate {Subject}",
                        e.Accept ? "Accepted" : "Rejected", e.Certificate.Subject);
                }
            };

            await config.CertificateValidator.Update(config.SecurityConfiguration).ConfigureAwait(false);

            // Set Certificate
            try
            {
                // just take the public key
                Certificate = new X509Certificate2(
                    config.SecurityConfiguration.ApplicationCertificate.Certificate.RawData);
            }
            catch
            {
                Certificate = config.SecurityConfiguration.ApplicationCertificate.Certificate;
            }

            _logger.LogInformation("Starting server ...");
            // start the server.
            await application.Start(_server).ConfigureAwait(false);

            foreach (var ep in config.ServerConfiguration.BaseAddresses)
            {
                _logger.LogInformation("Listening on {Endpoint}", ep);
            }

            _logger.LogInformation("Server started.");
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

        private readonly ILogger _logger;
        private readonly IServerFactory _factory;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private ServerBase _server;
    }
}
