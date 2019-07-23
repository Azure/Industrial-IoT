using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using Opc.Ua;
    using System.Net.Http;
    using static OpcApplicationConfiguration;
    using static Program;

    public sealed class PlcOpcUaServer : IDisposable
    {
        public PlcOpcUaServer()
        {
            Uri dockerUri = null;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dockerUri = new Uri("tcp://localhost:2375");
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    dockerUri = new Uri("unix:///var/run/docker.sock");
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    dockerUri = new Uri("not supported");
                }
                _dockerClient = new DockerClientConfiguration(dockerUri).CreateClient();
            }
            catch
            {
                throw new Exception($"Please adjust your docker deamon endpoint '{dockerUri}' for your configuration.");
            }

            // cleanup all PLC containers
            CleanupContainerAsync().Wait();

            // pull the latest image
            ImagesCreateParameters createParameters = new ImagesCreateParameters();
            createParameters.FromImage = _plcImage;
            createParameters.Tag = "latest";
            try
            {
                _dockerClient.Images.CreateImageAsync(createParameters, new AuthConfig(), new Progress<JSONMessage>()).Wait();

            }
            catch (Exception)
            {
                throw new Exception($"Cannot pull image '{_plcImage}");
            }

            ImageInspectResponse imageInspectResponse = _dockerClient.Images.InspectImageAsync(_plcImage).Result;

            // create a new container
            CreateContainerParameters containerParams = new CreateContainerParameters();
            containerParams.Image = _plcImage;
            containerParams.Hostname = "opcplc";
            containerParams.Name = "opcplc";
            containerParams.Cmd = new string[]
            {
                "--aa",
                "--pn", $"{_plcPort}"
            };
            // workaround .NET2.1 issue for private key access
            if (imageInspectResponse.Os.Equals("windows", StringComparison.InvariantCultureIgnoreCase))
            {
                containerParams.Cmd.Add("--at");
                containerParams.Cmd.Add("X509Store");
            }
            containerParams.ExposedPorts = new Dictionary<string, EmptyStruct>();
            containerParams.ExposedPorts.Add(new KeyValuePair<string, EmptyStruct>($"{_plcPort}/tcp", new EmptyStruct()));
            containerParams.HostConfig = new HostConfig();
            PortBinding portBinding = new PortBinding();
            portBinding.HostPort = _plcPort;
            portBinding.HostIP = null;
            List<PortBinding> portBindings = new List<PortBinding>();
            portBindings.Add(portBinding);
            containerParams.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>();
            containerParams.HostConfig.PortBindings.Add($"{_plcPort}/tcp", portBindings);
            CreateContainerResponse response = null;
            try
            {
                response = _dockerClient.Containers.CreateContainerAsync(containerParams).Result;
                _plcContainerId = response.ID;
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                _dockerClient.Containers.StartContainerAsync(_plcContainerId, new ContainerStartParameters()).Wait();
            }
            catch (Exception)
            {
                throw;

            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        void Dispose(bool disposing)
        {
            try
            {
                CleanupContainerAsync().Wait();
            }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
            catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
            {

            }
            if (disposing)
            {
                // dispose managed resources
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task CleanupContainerAsync()
        {
            IList<ContainerListResponse> containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    Limit = 10,
                });

            foreach (var container in containers)
            {
                if (container.Image.Equals(_plcImage, StringComparison.InvariantCulture))
                {
                    try
                    {
                        await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                    }
                    catch (Exception)
                    {
                        throw new Exception($"Cannot stop the PLC container with id '{container.ID}'");
                    }
                    try
                    {
                        await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters());
                    }
                    catch (Exception)
                    {
                        throw new Exception($"Cannot remove the PLC container with id '{container.ID}'");
                    }
                }
            }
        }

        // when testing locally, spin up your own registry and put the image in here
        //string _plcImage = "localhost:5000/opc-plc";
        readonly string _plcImage = "mcr.microsoft.com/iotedge/opc-plc";
        readonly string _plcPort = "50000";
        readonly DockerClient _dockerClient;
        readonly string _plcContainerId = string.Empty;
    }

    public sealed class PlcOpcUaServerFixture : IDisposable
    {
        public PlcOpcUaServer Plc { get; private set; }

        public PlcOpcUaServerFixture()
        {
            try
            {
                Plc = new PlcOpcUaServer();
            }
            catch
            {
                Plc = null;
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                Plc?.Dispose();
                Plc = null;
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public sealed class TestDirectoriesFixture
    {

        public TestDirectoriesFixture()
        {
            try
            {
                if (!Directory.Exists($"{Directory.GetCurrentDirectory()}/tempdata"))
                {
                    Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}/tempdata");
                }
            }
            catch (Exception)
            {
                throw;
            }
            try
            {
                if (File.Exists(LogFileName))
                {
                    File.Delete(LogFileName);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

    }

    public sealed class OpcPublisherFixture : IDisposable
    {

        public OpcPublisherFixture()
        {
            // init publisher logging
            //LogLevel = "debug";
            LogLevel = "info";
            if (Logger == null)
            {
                InitLogging();
            }

            // init publisher application configuration
            AutoAcceptCerts = true;
            // mitigation for bug in .NET Core 2.1
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OpcOwnCertStoreType = CertificateStoreType.X509Store;
                OpcOwnCertStorePath = OpcOwnCertX509StorePathDefault;
            }
            if (_opcApplicationConfiguration == null)
            {
                _opcApplicationConfiguration = new OpcApplicationConfiguration();
                _opcApplicationConfiguration.ConfigureAsync().Wait();
            }

            // configure hub communication
            HubCommunicationBase.DefaultSendIntervalSeconds = 0;
            HubCommunicationBase.HubMessageSize = 0;
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                _opcApplicationConfiguration = null;
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static OpcApplicationConfiguration _opcApplicationConfiguration = null;
    }
}
