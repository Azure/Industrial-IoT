// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Simulation.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Infrastructure.Compute;
    using Microsoft.Azure.IIoT.Net;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Extensions.Configuration;
    using Docker.DotNet;
    using Docker.DotNet.X509;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a fully customized simulation vm that contains docker
    /// and edge components.
    /// </summary>
    internal class EdgeSimulation : ISimulation {

        /// <inheritdoc/>
        public string EdgeDeviceId => EdgeCs.DeviceId;

        /// <inheritdoc/>
        public string Name => Vm.Name;

        /// <summary>
        /// The docker tls certificate to use
        /// </summary>
        public X509Certificate2 TlsCert => GetTlsCert().Result;

        /// <summary>
        /// The virtual machine instance
        /// </summary>
        public IVirtualMachineResource Vm { get; }

        /// <summary>
        /// The edge connection string
        /// </summary>
        public ConnectionString EdgeCs { get; }

        /// <summary>
        /// Create simulation vm
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="edgeCs"></param>
        /// <param name="logger"></param>
        public EdgeSimulation(IVirtualMachineResource vm, ConnectionString edgeCs,
            IIoTHubTwinServices twin, ILogger logger) {

            Vm = vm ?? throw new ArgumentNullException(nameof(vm));
            EdgeCs = edgeCs ?? throw new ArgumentNullException(nameof(edgeCs));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));

            _logger.Info(Vm.IPAddress, () => { });
        }

        /// <inheritdoc/>
        public Task<ISecureShell> OpenSecureShellAsync() =>
            Vm.OpenShellAsync(new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token);

        /// <inheritdoc/>
        public ISimulatedDevice CreateDevice(DeviceType type,
            IConfiguration configuration) {

            // TODO:


            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<bool> IsEdgeRunningAsync() {
            using (var shell = await OpenSecureShellAsync()) {
                await GetTlsCert(shell);
                var result = await shell.ExecuteCommandAsync(
                    "systemctl is-active iotedge", TimeSpan.FromSeconds(30));
                return result.Trim().EqualsIgnoreCase("active");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsEdgeConnectedAsync() {
            try {
                await Retry.Do(_logger, CancellationToken.None, async () => {
                    var response = await _twin.CallMethodAsync(EdgeDeviceId, "$edgeAgent",
                        new Hub.Models.MethodParameterModel { Name = "ping" });
                    if (response.Status != 200) {
                        throw new ExternalDependencyException("Ping failed");
                    }
                }, ex => !(ex is ResourceNotFoundException), Retry.Linear, 20);
                return true;
            }
            catch {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task ResetEdgeAsync() {
            using (var shell = await OpenSecureShellAsync()) {
                await GetTlsCert(shell);
                var result = await shell.ExecuteCommandAsync(
                    "sudo systemctl restart iotedge");
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetEdgeLogAsync() {
            using (var shell = await OpenSecureShellAsync()) {
                await GetTlsCert(shell);
                return await shell.ExecuteCommandAsync(
                    "journalctl -u iotedge --no-pager", TimeSpan.FromMinutes(1));
            }
        }

        /// <inheritdoc/>
        public Task RestartAsync() => Vm.RestartAsync();

        /// <summary>
        /// A docker client to use
        /// </summary>
        public async Task<DockerClient> OpenDockerClientAsync() {
            var credentials = new CertificateCredentials(TlsCert);
            credentials.ServerCertificateValidationCallback +=
                (o, c, ch, er) => true;

            var config = new DockerClientConfiguration(
                new Uri($"tcp://{Vm.IPAddress}:2376"), credentials);
            var client = config.CreateClient();

            var version = await client.System.GetVersionAsync();
            var system = await client.System.GetSystemInfoAsync();
            _logger.Debug(
                $"Created docker client (version {version.Version}).",
                    () => system.ServerVersion);

            return client;
        }

        /// <summary>
        /// Return the created tls auth certificate from the virtual machine
        /// and if it does not exist install Docker and iot edge first, then
        /// try again.
        /// </summary>
        /// <returns></returns>
        internal async Task<X509Certificate2> GetTlsCert() {
            if (_tlsCert != null) {
                return _tlsCert;
            }
            using (var shell = await OpenSecureShellAsync()) {
                return await GetTlsCert(shell);
            }
        }

        /// <summary>
        /// Return the created tls auth certificate from the virtual machine
        /// and if it does not exist install Docker and iot edge first, then
        /// try again.
        /// </summary>
        /// <returns></returns>
        internal async Task<X509Certificate2> GetTlsCert(ISecureShell shell) {
            if (_tlsCert != null) {
                return _tlsCert;
            }
            _tlsCert = await DownloadTlsCertAsync(shell);
            if (_tlsCert != null) {
                return _tlsCert;
            }

            _logger.Debug($"Installing dependencies on {Vm.IPAddress}...",
                () => { });
            await InstallDependencies(shell);

            // Try again...
            _logger.Debug($"Downloading certificates from {Vm.IPAddress}...",
                () => { });
            return _tlsCert = await DownloadTlsCertAsync(shell);
        }

        /// <summary>
        /// Download tls cert
        /// </summary>
        /// <param name="shell"></param>
        /// <returns></returns>
        private static async Task<X509Certificate2> DownloadTlsCertAsync(ISecureShell shell) {
            try {
                var keyPfxBuffLength = 10000;
                var keyPfxContent = new byte[keyPfxBuffLength];
                var loaded = await shell.DownloadAsync(keyPfxContent, keyPfxBuffLength,
                    "key.pfx", $"{kInstallPath}/tls", true);
                if (loaded == 0) {
                    // No cert - installation must have failed or not have happened.
                    return null;
                }
                return new X509Certificate2(keyPfxContent);
            }
            catch {
                return null;
            }
        }

        /// <summary>
        /// Install docker and iot edge.
        /// </summary>
        /// <param name="shell"></param>
        private async Task InstallDependencies(ISecureShell shell) {
            await shell.UploadAsync(InstallDockerScript("ubuntu-xenial"), "install_docker.sh",
                kInstallPath, true, "+x");
            await shell.UploadAsync(CreateOpenSslCerts(Vm.IPAddress), "create_certs.sh",
                kInstallPath, true, "+x");
            await shell.UploadAsync(InstallSslCert(), "install_certs.sh",
                kInstallPath, true, "+x");
            await shell.UploadAsync(DockerConfigTlsEnable(), "dockerd_tls.config",
                kInstallPath, true);
            await shell.UploadAsync(CreateDockerOptsTls(false), "enable_tls.sh",
                kInstallPath, true, "+x");
            await shell.UploadAsync(DockerConfigTlsDisabled(), "dockerd_notls.config",
                kInstallPath, true, "+x");
            await shell.UploadAsync(CreateDockerOptsTls(true), "disable_tls.sh",
                kInstallPath, true, "+x");
            await shell.UploadAsync(InstallIoTEdge("ubuntu", "16.04"), "install_iotedge.sh",
                kInstallPath, true, "+x");
            await shell.UploadAsync(IoTEdgeConfigYamlManual(EdgeCs.ToString()),
                "iotedge_config.yaml", kInstallPath, true);

            _logger.Debug("Installing Docker...", () => { });
            await shell.ExecuteCommandAsync($"bash -c ~/{kInstallPath}/install_docker.sh");
            _logger.Debug("Creating OpenSSL certificates...", () => { });
            await shell.ExecuteCommandAsync($"bash -c ~/{kInstallPath}/create_certs.sh");
            _logger.Debug("Installing new certificates...", () => { });
            await shell.ExecuteCommandAsync($"bash -c ~/{kInstallPath}/install_certs.sh");
            _logger.Debug("Locking down docker to authorized use...", () => { });
            await shell.ExecuteCommandAsync($"bash -c ~/{kInstallPath}/enable_tls.sh");
            _logger.Debug("Installing iot edge...", () => { });
            await shell.ExecuteCommandAsync($"bash -c ~/{kInstallPath}/install_iotedge.sh");
        }

        /// <summary>
        /// Installs Docker Engine and tools and adds current user to the
        /// docker group.
        /// </summary>
        internal static byte[] InstallDockerScript(string distribution) =>
            ToUnixAscii($@"#!/bin/bash
set -x
if [ ! -d ~/{kInstallPath}/tls ]; then mkdir -p ~/{kInstallPath}/tls ; fi
sudo apt-get update
sudo apt-get install -y --no-install-recommends apt-transport-https ca-certificates curl software-properties-common
curl -fsSL https://apt.dockerproject.org/gpg | sudo apt-key add -
sudo add-apt-repository ""deb https://apt.dockerproject.org/repo/ {distribution} main""
sudo apt-get update
sudo apt-get -y install docker-engine
sudo groupadd docker
sudo usermod -aG docker $USER
");
        /// <summary>
        /// Linux bash script that creates the TLS certificates for a secured
        /// Docker connection.
        /// </summary>
        internal static byte[] CreateOpenSslCerts(string host) =>
            ToUnixAscii($@"#!/bin/bash
set -x
if [ ! -d ~/{kInstallPath}/tls ]; then rm -f -r ~/{kInstallPath}/tls ; fi
mkdir -p ~/{kInstallPath}/tls
cd ~/{kInstallPath}/tls

# Generate CA certificate
openssl genrsa -passout pass:$CERT_CA_PWD_PARAM$ -aes256 -out ca-key.pem 2048

# Generate Server certificates
openssl req -passin pass:$CERT_CA_PWD_PARAM$ -subj '/CN=Docker Host CA/C=US' -new -x509 -days 365 -key ca-key.pem -sha256 -out ca.pem
openssl genrsa -out server-key.pem 2048
openssl req -subj '/CN={host}' -sha256 -new -key server-key.pem -out server.csr
echo subjectAltName = DNS:{host} IP:127.0.0.1 > extfile.cnf
openssl x509 -req -passin pass:$CERT_CA_PWD_PARAM$ -days 365 -sha256 -in server.csr -CA ca.pem -CAkey ca-key.pem -CAcreateserial -out server.pem -extfile extfile.cnf

# Generate Client certificates
openssl genrsa -passout pass:$CERT_CA_PWD_PARAM$ -out key.pem
openssl req -passin pass:$CERT_CA_PWD_PARAM$ -subj '/CN=client' -new -key key.pem -out client.csr
echo extendedKeyUsage = clientAuth,serverAuth > extfile.cnf
openssl x509 -req -passin pass:$CERT_CA_PWD_PARAM$ -days 365 -sha256 -in client.csr -CA ca.pem -CAkey ca-key.pem -CAcreateserial -out cert.pem -extfile extfile.cnf

# Generate .PFX key file to be used when connecting with the .Net Docker client
openssl pkcs12 -export -inkey key.pem -in cert.pem -out key.pfx -passout pass: -CAfile ca.pem
cd ~
");
        /// <summary>
        /// Bash script that sets up the TLS certificates to be used in a secured
        /// Docker configuration file
        /// must be run on the Docker dockerHostUrl after the VM is provisioned.
        /// </summary>
        internal static byte[] InstallSslCert() =>
            ToUnixAscii($@"#!/bin/bash
set -x
echo ""if [ ! -d /etc/docker/tls ]; then sudo mkdir -p /etc/docker/tls ; fi""
if [ ! -d /etc/docker/tls ]; then sudo mkdir -p /etc/docker/tls ; fi
echo sudo cp -f ~/{kInstallPath}/tls/ca.pem /etc/docker/tls/ca.pem
sudo cp -f ~/{kInstallPath}/tls/ca.pem /etc/docker/tls/ca.pem
echo sudo cp -f ~/{kInstallPath}/tls/server.pem /etc/docker/tls/server.pem
sudo cp -f ~/{kInstallPath}/tls/server.pem /etc/docker/tls/server.pem
echo sudo cp -f ~/{kInstallPath}/tls/server-key.pem /etc/docker/tls/server-key.pem
sudo cp -f ~/{kInstallPath}/tls/server-key.pem /etc/docker/tls/server-key.pem
echo sudo chmod -R 755 /etc/docker
sudo chmod -R 755 /etc/docker
");
        /// <summary>
        /// Docker daemon config file allowing connections from any Docker client.
        /// </summary>
        internal static byte[] DockerConfigTlsEnable() =>
            ToUnixAscii($@"
[Service]
ExecStart=
ExecStart=/usr/bin/dockerd --tlsverify --tlscacert=/etc/docker/tls/ca.pem --tlscert=/etc/docker/tls/server.pem --tlskey=/etc/docker/tls/server-key.pem -H tcp://0.0.0.0:2376 -H unix:///var/run/docker.sock
");
        /// <summary>
        /// Docker daemon config file allowing connections from any Docker client.
        /// </summary>
        internal static byte[] DockerConfigTlsDisabled() =>
            ToUnixAscii($@"
[Service]
ExecStart=
ExecStart=/usr/bin/dockerd --tls=false -H tcp://0.0.0.0:2375 -H unix:///var/run/docker.sock
");
        /// <summary>
        /// Bash script that creates a default unsecured Docker configuration file.
        /// must be run on the Docker dockerHostUrl after the VM is provisioned.
        /// </summary>
        internal static byte[] CreateDockerOptsTls(bool disable) =>
            ToUnixAscii($@"#!/bin/bash
set -x
sudo service docker stop
if [ ! -d /etc/systemd/system/docker.service.d ]; then sudo mkdir -p /etc/systemd/system/docker.service.d ; fi
sudo cp -f ~/{kInstallPath}/dockerd_{(disable ? "no" : "")}tls.config /etc/systemd/system/docker.service.d/custom.conf
sudo systemctl daemon-reload
sudo service docker start
");
        /// <summary>
        /// Installs Docker Engine and tools and adds current user to the
        /// docker group.
        /// </summary>
        internal static byte[] InstallIoTEdge(string linux, string version) =>
            ToUnixAscii($@"#!/bin/bash
# Install repository configuration
cd ~/{kInstallPath}
curl https://packages.microsoft.com/config/{linux}/{version}/prod.list > ./microsoft-prod.list
sudo cp -f ./microsoft-prod.list /etc/apt/sources.list.d/

# Install Microsoft GPG public key
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo cp -f ./microsoft.gpg /etc/apt/trusted.gpg.d/

# Perform apt upgrade
sudo apt-get -y update
sudo apt-get -y upgrade
sudo apt-get -y install iotedge
sudo sed -i '$ a IOTEDGE_HOMEDIR=/var/lib/iotedge' /etc/environment
sudo cp -f ~/{kInstallPath}/iotedge_config.yaml /etc/iotedge/config.yaml
sudo systemctl restart iotedge
cd ~
");
        /// <summary>
        /// IoT Edge yaml configuration manual configuration
        /// </summary>
        internal static byte[] IoTEdgeConfigYamlManual(string cs) =>
            ToUnixAscii($@"
provisioning:
  source: ""manual""
  device_connection_string: ""{cs}""
");
        /// <summary>
        /// IoT Edge yaml configuration for automatic registration
        /// </summary>
        internal static byte[] IoTEdgeConfigYamlAuto(string scopeId,
            string registrationId) =>
            ToUnixAscii($@"
provisioning:
  source: ""dps""
  global_endpoint: ""https://global.azure-devices-provisioning.net""
  scope_id: ""{scopeId}""
  registration_id: ""{registrationId}""
");

        /// <summary>
        /// Helper to convert string literal script to unix
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        private static byte[] ToUnixAscii(string script) =>
            Encoding.ASCII.GetBytes(script.Replace('\r', ' '));

        const string kInstallPath = ".simulation";
        private X509Certificate2 _tlsCert;
        private readonly ILogger _logger;
        private readonly IIoTHubTwinServices _twin;
    }
}
