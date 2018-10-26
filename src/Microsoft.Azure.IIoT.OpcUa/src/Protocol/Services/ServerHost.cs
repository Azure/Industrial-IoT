// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Console host for servers
    /// </summary>
    public class ServerHost : IServerHost {

        /// <inheritdoc/>
        public bool AutoAccept { get; set; }

        /// <summary>
        /// Create server console host
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="factory"></param>
        public ServerHost(IServerFactory factory, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            if (_server != null) {
                try {
                    await _lock.WaitAsync();
                    if (_server != null) {
                        _logger.Info($"Stopping server.");
                        try {
                            _server.Stop();
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception se) {
                            _logger.Error("Server not cleanly stopped.",
                                () => se);
                        }
                        _server.Dispose();
                    }
                    _logger.Info($"Server stopped.");
                }
                catch (Exception ce) {
                    _logger.Error("Stopping server caused exception.",
                        () => ce);
                }
                finally {
                    _server = null;
                    _lock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(IEnumerable<int> ports) {
            if (_server == null) {
                try {
                    await _lock.WaitAsync();
                    if (_server == null) {
                        await StartServerInternal(ports);
                        return;
                    }
                }
                catch (Exception ex) {
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
        public void Dispose() => StopAsync().Wait();

        /// <summary>
        /// Start server
        /// </summary>
        /// <param name="ports"></param>
        /// <returns></returns>
        private async Task StartServerInternal(IEnumerable<int> ports) {
            _logger.Info("Starting server...");
            ApplicationInstance.MessageDlg = new DummyDialog();

            var config = _factory.CreateServer(ports, out _server);

            config = ApplicationInstance.FixupAppConfig(config);
            await config.Validate(ApplicationType.Server);
            config.CertificateValidator.CertificateValidation += (v, e) => {
                if (e.Error.StatusCode ==
                    StatusCodes.BadCertificateUntrusted) {
                    e.Accept = AutoAccept;
                    _logger.Info((e.Accept ? "Accepted" : "Rejected") +
                        $" Certificate {e.Certificate.Subject}");
                }
            };

            await config.CertificateValidator.Update(config.SecurityConfiguration);
            // Use existing certificate, if it is there.
            var cert = await config.SecurityConfiguration.ApplicationCertificate
                .Find(true);
            if (cert == null) {
                // Create cert
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
                throw new Exception(
                    "Application instance certificate invalid!");
            }

            // start the server.
            await application.Start(_server);

            foreach (var ep in config.ServerConfiguration.BaseAddresses) {
                _logger.Info($"Listening on {ep}");
            }
            _logger.Info("Server started.");
        }

        /// <inheritdoc/>
        private class DummyDialog : IApplicationMessageDlg {
            /// <inheritdoc/>
            public override void Message(string text, bool ask) { }
            /// <inheritdoc/>
            public override Task<bool> ShowAsync() => Task.FromResult(true);
        }

        private readonly ILogger _logger;
        private readonly IServerFactory _factory;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private ServerBase _server;
    }
}
