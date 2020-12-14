// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Serilog;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Console host for servers
    /// </summary>
    public class ServerConsoleHost : IServerHost {

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
        public ServerConsoleHost(IServerFactory factory, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            if (_server != null) {
                await _lock.WaitAsync();
                try {
                    if (_server != null) {
                        _logger.Information("Stopping server.");
                        try {
                            _server.Stop();
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception se) {
                            _logger.Error(se, "Server not cleanly stopped.");
                        }
                        _server.Dispose();
                    }
                    _logger.Information("Server stopped.");
                }
                catch (Exception ce) {
                    _logger.Error(ce, "Stopping server caused exception.");
                }
                finally {
                    _server = null;
                    Certificate = null;
                    _lock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(IEnumerable<int> ports) {
            if (_server == null) {
                await _lock.WaitAsync();
                try {
                    if (_server == null) {
                        await StartServerInternalAsync(ports, PkiRootPath);
                        return;
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Starting server caused exception.");
                    _server?.Dispose();
                    _server = null;
                    throw ;
                }
                finally {
                    _lock.Release();
                }
            }
            throw new InvalidOperationException("Already started");
        }

        /// <inheritdoc/>
        public void Dispose() {
            StopAsync().Wait();
            _lock.Dispose();
        }

        /// <summary>
        /// Start server
        /// </summary>
        /// <param name="ports"></param>
        /// <param name="pkiRootPath"></param>
        /// <returns></returns>
        private async Task StartServerInternalAsync(IEnumerable<int> ports, string pkiRootPath) {
            ApplicationInstance.MessageDlg = new DummyDialog();

            var config = _factory.CreateServer(ports, pkiRootPath, out _server);
            _logger.Information("Server created...");

            config.SecurityConfiguration.AutoAcceptUntrustedCertificates = AutoAccept;
            config = ApplicationInstance.FixupAppConfig(config);

            _logger.Information("Validate configuration...");
            await config.Validate(config.ApplicationType);

            _logger.Information("Initialize certificate validation...");
            var application = new ApplicationInstance(config);

            // check the application certificate.
            var hasAppCertificate =
                await application.CheckApplicationInstanceCertificate(true,
                    CertificateFactory.DefaultKeySize);
            if (!hasAppCertificate) {
                _logger.Error("Failed validating own certificate!");
                throw new Exception("Application instance certificate invalid!");
            }

            config.CertificateValidator.CertificateValidation += (v, e) => {
                if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                    e.Accept = AutoAccept;
                    _logger.Information((e.Accept ? "Accepted" : "Rejected") +
                        " Certificate {subject}", e.Certificate.Subject);
                }
            };

            await config.CertificateValidator.Update(config.SecurityConfiguration);

            // Set Certificate
            try {
                // just take the public key
                Certificate = new X509Certificate2(config.SecurityConfiguration.ApplicationCertificate.Certificate.RawData);
            }
            catch {
                Certificate = config.SecurityConfiguration.ApplicationCertificate.Certificate;
            }

            _logger.Information("Starting server ...");
            // start the server.
            await application.Start(_server);

            foreach (var ep in config.ServerConfiguration.BaseAddresses) {
                _logger.Information("Listening on {ep}", ep);
            }
            _logger.Information("Server started.");
        }

        /// <inheritdoc/>
        private class DummyDialog : IApplicationMessageDlg {
            /// <inheritdoc/>
            public override void Message(string text, bool ask) { }
            /// <inheritdoc/>
            public override Task<bool> ShowAsync() {
                return Task.FromResult(true);
            }
        }

        private readonly ILogger _logger;
        private readonly IServerFactory _factory;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private ServerBase _server;
    }
}
