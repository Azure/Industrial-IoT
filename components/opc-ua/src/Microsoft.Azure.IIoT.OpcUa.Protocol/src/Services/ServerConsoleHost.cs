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
    using System.Runtime.InteropServices;

    /// <summary>
    /// Console host for servers
    /// </summary>
    public class ServerConsoleHost : IServerHost {

        /// <inheritdoc/>
        public X509Certificate2 Certificate { get; private set; }

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
                        await StartServerInternalAsync(ports);
                        return;
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Starting server caused exception.");
                    _server?.Dispose();
                    _server = null;
                    throw ex;
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
        /// <returns></returns>
        private async Task StartServerInternalAsync(IEnumerable<int> ports) {
            ApplicationInstance.MessageDlg = new DummyDialog();

            var config = _factory.CreateServer(ports, out _server);
            _logger.Information("Server created...");

            config = ApplicationInstance.FixupAppConfig(config);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                config.SecurityConfiguration.ApplicationCertificate.StoreType =
                    CertificateStoreType.X509Store;
                config.SecurityConfiguration.ApplicationCertificate.StorePath =
                    "CurrentUser\\UA_MachineDefault";
            }
            _logger.Information("Validate configuration...");
            await config.Validate(ApplicationType.Server);

            config.CertificateValidator = new CertificateValidator();
            config.CertificateValidator.CertificateValidation += (v, e) => {
                if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                    e.Accept = AutoAccept;
                    _logger.Information((e.Accept ? "Accepted" : "Rejected") +
                        " Certificate {subject}", e.Certificate.Subject);
                }
            };

            _logger.Information("Initialize certificate validation...");
            await config.CertificateValidator.Update(config.SecurityConfiguration);

            // Use existing certificate, if it is there.
            var cert = await config.SecurityConfiguration.ApplicationCertificate
                .Find(true);
            if (cert == null) {
                _logger.Information("Creating new certificate in {path}...",
                    config.SecurityConfiguration.ApplicationCertificate.StorePath);
                // Create cert
#pragma warning disable IDE0067 // Dispose objects before losing scope
                cert = CertificateFactory.CreateCertificate(
                    config.SecurityConfiguration.ApplicationCertificate.StoreType,
                    config.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null, config.ApplicationUri, config.ApplicationName,
                    config.SecurityConfiguration.ApplicationCertificate.SubjectName,
                    null, CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize,
                    false, null, null);
#pragma warning restore IDE0067 // Dispose objects before losing scope
            }

            if (cert != null) {
                config.SecurityConfiguration.ApplicationCertificate.Certificate = cert;
                config.ApplicationUri = Utils.GetApplicationUriFromCertificate(cert);
            }

            var application = new ApplicationInstance(config);

            // check the application certificate.
            var haveAppCertificate =
                await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate) {
                _logger.Error("Failed validating certificate!");
                throw new Exception("Application instance certificate invalid!");
            }

            // Set certificate
            Certificate = cert;

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
