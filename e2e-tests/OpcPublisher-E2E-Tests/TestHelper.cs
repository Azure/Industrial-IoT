// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests
{
    using Azure;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Messaging.EventHubs.Consumer;
    using Azure.ResourceManager;
    using Azure.ResourceManager.ContainerInstance;
    using Azure.ResourceManager.ContainerInstance.Models;
    using Azure.ResourceManager.Resources;
    using Azure.ResourceManager.Storage;
    using Azure.Storage;
    using Azure.Storage.Files.Shares;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using OpcPublisherAEE2ETests.Config;
    using Renci.SshNet;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;

    public record class MethodResultModel(string JsonPayload, int Status);
    public record class MethodParameterModel
    {
        public string Name { get; set; }
        public string JsonPayload { get; set; }
    }

    internal static class TestHelper
    {
        /// <summary>
        /// Update Device Twin tag
        /// </summary>
        /// <param name="patch">Name of deployed Industrial IoT</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task UpdateTagAsync(
            string patch,
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        )
        {
            var registryManager = context.RegistryHelper.RegistryManager;
            var twin = await registryManager.GetTwinAsync(context.DeviceConfig.DeviceId, ct).ConfigureAwait(false);
            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="json">String for published_nodes.json</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task SwitchToStandaloneModeAndPublishNodesAsync(
            string json,
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        )
        {
            context.OutputHelper.WriteLine("Write published_nodes.json to IoT Edge");
            context.OutputHelper.WriteLine(json);
            await PublishNodesAsync(json, context, ct).ConfigureAwait(false);
            await SwitchToStandaloneModeAsync(context, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="json">String for published_nodes.json</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task PublishNodesAsync(
            string json,
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        )
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await CreateFolderOnEdgeVMAsync(TestConstants.PublishedNodesFolder, context, ct).ConfigureAwait(false);
                    using var scpClient = await CreateScpClientAndConnectAsync(context, ct).ConfigureAwait(false);
                    await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    scpClient.Upload(stream, TestConstants.PublishedNodesFullName);

                    return;
                }
                catch (Exception ex) when (attempt < 60)
                {
                    context.OutputHelper.WriteLine($"Failed to write {TestConstants.PublishedNodesFullName} to {TestConstants.PublishedNodesFolder} on host {context.SshConfig.Host} with username {context.SshConfig.Username} ({ex.Message})");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Clean published nodes JSON files.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task CleanPublishedNodesJsonFilesAsync(IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    // Make sure directories exist.
                    using (var sshCient = await CreateSshClientAndConnectAsync(context, ct).ConfigureAwait(false))
                    {
                        sshCient.RunCommand($"[ ! -d {TestConstants.PublishedNodesFolder} ]" +
                            $" && sudo mkdir -m 777 -p {TestConstants.PublishedNodesFolder}");
                    }
                    break;
                }
                catch (Exception ex) when (attempt < 60)
                {
                    context.OutputHelper.WriteLine($"Failed to create folder on host {context.SshConfig.Host} with username {context.SshConfig.Username} ({ex.Message})");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }
            await PublishNodesAsync("[]", context, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the unmanaged-Tag to "true" to enable Standalone-Mode
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        public static async Task SwitchToStandaloneModeAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        )
        {
            const string patch =
                @"{
                    tags: {
                        unmanaged: true
                    }
                }";

            await UpdateTagAsync(patch, context, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new SshClient based on SshConfig and directly connects to host
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct"></param>
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static async Task<SshClient> CreateSshClientAndConnectAsync(IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            var privateKeyFile = GetPrivateSshKey(context);
            try
            {
                var client = new SshClient(
                    context.SshConfig.Host,
                    context.SshConfig.Username,
                    privateKeyFile);

                var connectAttempt = 0;
                while (true)
                {
                    try
                    {
                        await client.ConnectAsync(ct);
                        return client;
                    }
                    catch (SocketException) when (++connectAttempt < 10)
                    {
                        await Task.Delay(3000, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                context.OutputHelper.WriteLine($"Failed to open ssh connection to host {context.SshConfig.Host} with username {context.SshConfig.Username} ({ex.Message})");
                throw;
            }
        }

        /// <summary>
        /// Create a new ScpClient based on SshConfig and directly connects to host
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct"></param>
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static async Task<ScpClient> CreateScpClientAndConnectAsync(IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            var privateKeyFile = GetPrivateSshKey(context);
            try
            {
                var client = new ScpClient(
                    context.SshConfig.Host,
                    context.SshConfig.Username,
                    privateKeyFile);

                var connectAttempt = 0;
                while (true)
                {
                    try
                    {
                        await client.ConnectAsync(ct);
                        return client;
                    }
                    catch (SocketException) when (++connectAttempt < 10)
                    {
                        await Task.Delay(3000, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                context.OutputHelper.WriteLine("Failed to open scp connection to host {0} with username {1} ({2})",
                    context.SshConfig.Host,
                    context.SshConfig.Username, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the private SSH key from the configuration to connect to the Edge VM
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns></returns>
        private static PrivateKeyFile GetPrivateSshKey(IIoTPlatformTestContext context)
        {
            var buffer = Encoding.Default.GetBytes(context.SshConfig.PrivateKey);
            var privateKeyStream = new MemoryStream(buffer);

            return new PrivateKeyFile(privateKeyStream);
        }

        /// <summary>
        /// Delete a file on the Edge VM
        /// </summary>
        /// <param name="fileName">Filename of the file to delete</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct"></param>
        public static async Task DeleteFileOnEdgeVMAsync(string fileName, IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            var isSuccessful = false;
            using var client = await CreateSshClientAndConnectAsync(context, ct).ConfigureAwait(false);

            var terminal = client.RunCommand("rm " + fileName);

            if (string.IsNullOrEmpty(terminal.Error) ||
                terminal.Error.Contains("no such file", StringComparison.OrdinalIgnoreCase))
            {
                isSuccessful = true;
            }
            Assert.True(isSuccessful, "Delete file was not successful");
        }

        /// <summary>
        /// Create a folder on Edge VM (if not exists)
        /// </summary>
        /// <param name="folderPath">Name of the folder to create.</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct"></param>
        private static async Task CreateFolderOnEdgeVMAsync(string folderPath, IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            Assert.False(string.IsNullOrWhiteSpace(folderPath));

            var isSuccessful = false;
            using var client = await CreateSshClientAndConnectAsync(context, ct).ConfigureAwait(false);

            var terminal = client.RunCommand("sudo mkdir -p " + folderPath + ";cd " + folderPath + "; sudo chmod 777 " + folderPath);

            if (string.IsNullOrEmpty(terminal.Error) || terminal.Error.Contains("File exists", StringComparison.Ordinal))
            {
                isSuccessful = true;
            }

            Assert.True(isSuccessful, $"Folder creation was not successful because of {terminal.Error}");
        }

        /// <summary>
        /// Serialize a published nodes json file.
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="port">Port of OPC UA server</param>
        /// <param name="writerId">DataSetWriterId to set</param>
        /// <param name="opcNodes">OPC UA nodes</param>
        public static string PublishedNodesJson(this IIoTStandaloneTestContext context, uint port, string writerId, JArray opcNodes)
        {
            return JsonConvert.SerializeObject(
                new JArray(
                    context.PlcAciDynamicUrls.Select(host => new JObject(
                        new JProperty("EndpointUrl", $"opc.tcp://{host}:{port}"),
                        new JProperty("UseSecurity", true),
                        new JProperty("DataSetWriterGroup", Guid.NewGuid().ToString()),
                        new JProperty("DataSetWriterId", writerId),
                        new JProperty("OpcNodes", opcNodes)))
                ), Formatting.Indented);
        }

        /// <summary>
        /// Create an ACI
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="commandLine">Command line for container</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="fileToUpload">File to upload to the container</param>
        /// <param name="numInstances">Number of instances</param>
        public static async Task CreateSimulationContainerAsync(IIoTPlatformTestContext context,
            List<string> commandLine, CancellationToken cancellationToken, string fileToUpload = null, int numInstances = 1)
        {
            var azure = await GetAzureContextAsync(context, cancellationToken).ConfigureAwait(false);

            if (fileToUpload != null)
            {
                await UploadFileToStorageAccountAsync(context.AzureStorageName, context.AzureStorageKey, fileToUpload).ConfigureAwait(false);
            }

            context.PlcAciDynamicUrls = await Task.WhenAll(
                Enumerable.Range(0, numInstances)
                    .Select(i =>
                        CreateContainerGroupAsync(azure,
                            $"e2etesting-simulation-aci-{i}-{context.TestingSuffix}-dynamic",
                            context.PLCImage,
                            commandLine[0],
                            commandLine.GetRange(1, commandLine.Count - 1).ToArray(),
                            TestConstants.OpcSimulation.FileShareName,
                            context.AzureStorageName,
                            context.AzureStorageKey))).ConfigureAwait(false);
        }

        /// <summary>
        /// Upload a file to a storage account
        /// </summary>
        /// <param name="storageAccountName">Name of storage account</param>
        /// <param name="storageAccountKey">Key for storage account</param>
        /// <param name="fileName">File name</param>
        private async static Task UploadFileToStorageAccountAsync(string storageAccountName, string storageAccountKey, string fileName)
        {
            var share = new ShareClient(new Uri($"https://{storageAccountName}.file.core.windows.net/{TestConstants.OpcSimulation.FileShareName}"),
                new StorageSharedKeyCredential(storageAccountName, storageAccountKey));
            var directory = share.GetRootDirectoryClient();

            Assert.False(fileName.Contains('\\', StringComparison.Ordinal), "\\ can't be used for file path");

            // if fileName contains '/' we will extract the filename
            string onlyFileName;
            if (fileName.Contains('/', StringComparison.Ordinal))
            {
                onlyFileName = fileName[(fileName.LastIndexOf('/') + 1)..];
            }
            else
            {
                onlyFileName = fileName;
            }

            var cf = directory.GetFileClient(onlyFileName);
            await using var stream = new FileStream(fileName, FileMode.Open);
            await cf.UploadAsync(stream).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a container group
        /// </summary>
        /// <param name="resGroup">Resource group</param>
        /// <param name="containerGroupName">Container group name</param>
        /// <param name="containerImage">Container image</param>
        /// <param name="executable">Starting command line</param>
        /// <param name="commandLine">Additional command line options</param>
        /// <param name="fileShareName">File share name</param>
        /// <param name="storageAccountName">Storage account name</param>
        /// <param name="storageAccountKey">Storage account key</param>
        private static async Task<string> CreateContainerGroupAsync(ResourceGroupResource resGroup,
            string containerGroupName, string containerImage, string executable,
            string[] commandLine, string fileShareName, string storageAccountName,
            string storageAccountKey)
        {
            var azureRegion = (await resGroup.GetAvailableLocationsAsync(default)).Value.First();

            var container = new ContainerInstanceContainer(containerGroupName, containerImage,
                new ContainerResourceRequirements(new ContainerResourceRequestsContent(1.5, 1.0)));
            container.Command.Add(executable);
            container.Command.AddRange(commandLine);
            container.Ports.Add(new ContainerPort(50000));
            container.VolumeMounts.Add(new ContainerVolumeMount("share", "/app/files"));

            var containerGroup = new ContainerGroupData(azureRegion, container.YieldReturn(),
                ContainerInstanceOperatingSystemType.Linux);
            containerGroup.Volumes.Add(new ContainerVolume("share")
            {
                AzureFile = new ContainerInstanceAzureFileVolume(fileShareName, storageAccountName)
                {
                    StorageAccountKey = storageAccountKey,
                    IsReadOnly = false
                }
            });

            var result = await resGroup.GetContainerGroups().CreateOrUpdateAsync(WaitUntil.Completed,
                containerGroupName, containerGroup);

            //var containerGroup = await resGroup..ContainerGroups.Define(containerGroupName)
            //    .WithRegion(azureRegion)
            //    .WithExistingResourceGroup(resourceGroupName)
            //    .WithLinux()
            //    .WithPublicImageRegistryOnly()
            //    .DefineVolume("share")
            //        .WithExistingReadWriteAzureFileShare(fileShareName)
            //        .WithStorageAccountName(storageAccountName)
            //        .WithStorageAccountKey(storageAccountKey)
            //        .Attach()
            //    .DefineContainerInstance(containerGroupName)
            //        .WithImage(containerImage)
            //        .WithExternalTcpPort(50000)
            //        .WithCpuCoreCount(0.5)
            //        .WithMemorySizeInGB(0.5)
            //        .WithVolumeMountSetting("share", "/app/files")
            //        .WithStartingCommandLine(executable, commandLine)
            //        .Attach()
            //    .WithDnsPrefix(containerGroupName)
            //    .CreateAsync().ConfigureAwait(false);

            return result.Value.Data.IPAddress.Fqdn;
        }

        /// <summary>
        /// Delete an ACI
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static void DeleteSimulationContainer(IIoTPlatformTestContext context)
        {
            DeleteSimulationContainerAsync(context).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Delete an ACI
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static async Task DeleteSimulationContainerAsync(IIoTPlatformTestContext context)
        {
            if (context.PlcAciDynamicUrls == null || context.PlcAciDynamicUrls.Count == 0)
            {
                return;
            }
            await Task.WhenAll(context.PlcAciDynamicUrls
                .Select(url => url.Split(".")[0])
                .Select(async n => await (await context.AzureContext.GetContainerGroupAsync(n)).Value.DeleteAsync(WaitUntil.Completed))
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an azure context
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="InvalidOperationException"></exception>
        internal async static Task<ResourceGroupResource> GetAzureContextAsync(IIoTPlatformTestContext context, CancellationToken cancellationToken)
        {
            if (context.AzureContext != null)
            {
                return context.AzureContext;
            }

            SubscriptionResource subscription;
            ArmClient armClient;
            try
            {
                context.OutputHelper.WriteLine($"AZURE_CLIENT_ID: {Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")}");
                context.OutputHelper.WriteLine($"AZURE_CLIENT_SECRET: {Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")}");
                context.OutputHelper.WriteLine($"AZURE_TENANT_ID: {Environment.GetEnvironmentVariable("AZURE_TENANT_ID")}");
                var options = new DefaultAzureCredentialOptions
                {
                    TenantId = context.OpcPlcConfig.TenantId
                };
                //options.AdditionallyAllowedTenants.Add("*");
                armClient = new ArmClient(new DefaultAzureCredential(options));
                subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
            }
            catch
            {
                context.OutputHelper.WriteLine($"Obtaining access token from tenant {context.OpcPlcConfig.TenantId} using Azure CLI");
                armClient = new ArmClient(new AzureCliCredential());
                subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
            }

            if (!string.IsNullOrEmpty(context.OpcPlcConfig.SubscriptionId))
            {
                subscription = armClient.GetSubscriptionResource(new ResourceIdentifier(context.OpcPlcConfig.SubscriptionId));
            }

            var response = await subscription.GetResourceGroupAsync(context.OpcPlcConfig.ResourceGroupName, cancellationToken);
            var rg = Validate(context, response);
            var tags = await rg.GetTagResource().GetAsync(cancellationToken);
            context.OutputHelper.WriteLine($"Get tag from tags {tags}");
            var testingSuffix = Validate(context, tags).Data.TagValues[TestConstants.OpcSimulation.TestingResourcesSuffixName];
            context.TestingSuffix = testingSuffix;
            context.AzureStorageName = TestConstants.OpcSimulation.AzureStorageNameWithoutSuffix + testingSuffix;

            context.OutputHelper.WriteLine($"Get storage keys from {rg}");
            var storageAccount = await rg.GetStorageAccountAsync(context.AzureStorageName, cancellationToken: cancellationToken).ConfigureAwait(false);
            var keys = await Validate(context, storageAccount).GetKeysAsync(cancellationToken: cancellationToken).ToListAsync(cancellationToken);
            if (keys.Count == 0)
            {
                throw new InvalidOperationException($"No keys found for storage account {context.AzureStorageName}");
            }
            context.AzureStorageKey = keys[0].Value;

            context.OutputHelper.WriteLine($"Get container groups from {rg}");
            var firstAciIpAddress = context.OpcPlcConfig.Ips.Split(";")[0];
            var containerGroups = await rg.GetContainerGroups().ToListAsync(cancellationToken);
            context.OutputHelper.WriteLine($"Get container from groups {containerGroups}");
            var containerGroup = containerGroups.Find(g => g.Data.IPAddress.IP.ToString() == firstAciIpAddress);
            if (containerGroup == null)
            {
                throw new InvalidOperationException($"Container group with IP address {firstAciIpAddress} not found");
            }
            context.OutputHelper.WriteLine($"Get image from group {containerGroup}");
            if (containerGroup.Data.Containers.Count == 0)
            {
                throw new InvalidOperationException($"Container group with IP address {firstAciIpAddress} is empty");
            }
            context.PLCImage = containerGroup.Data.Containers[0].Image;
            context.AzureContext = rg;
            return rg;
        }

        private static T Validate<T>(IIoTPlatformTestContext context, Response<T> response)
        {
            if (!response.HasValue)
            {
                context.OutputHelper.WriteLine($"Get tags from {response}");
                throw new InvalidOperationException(response.ToString());
            }
            return response.Value;
        }

        /// <summary>
        /// Deserializes the JSON structure contained by the specified <see cref="PartitionEvent"/>
        /// into an instance of the specified type.
        /// </summary>
        /// <param name="partitionEvent">The <see cref="PartitionEvent"/> containing the object.</param>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <returns>The instance of <typeparamref name="T"/> being deserialized.</returns>
        public static T DeserializeJson<T>(this PartitionEvent partitionEvent)
        {
            using var sr = new StreamReader(partitionEvent.Data.BodyAsStream);
            using var reader = new JsonTextReader(sr);
            return kSerializer.Deserialize<T>(reader);
        }

        /// <summary>
        /// Get an Event Hub consumer
        /// </summary>
        /// <param name="config">Configuration for IoT Hub</param>
        /// <param name="consumerGroup"></param>
        public static EventHubConsumerClient GetEventHubConsumerClient(this IIoTHubConfig config, string consumerGroup = null)
        {
            return new EventHubConsumerClient(
                consumerGroup ?? TestConstants.TestConsumerGroupName,
                config.IoTHubEventHubConnectionString);
        }

        /// <summary>
        /// <para>
        ///   Reads events from all partitions of the IoT Hub Event Hubs endpoint as an asynchronous enumerable, allowing events to be iterated as they
        ///   become available on the partition, waiting as necessary should there be no events available.
        /// </para>
        /// <para>  Reading begins at the end of each partition seeing only new events as they are published.</para>
        /// <para>
        ///   Breaks up the batched messages contained in the event, and returns only messages for the provided
        ///   DataSetWriterId.
        /// </para>
        /// <para>
        ///   This enumerator may block for an indeterminate amount of time for an <c>await</c> if events are not available on the partition, requiring
        ///   cancellation via the <paramref name="cancellationToken"/> to be requested in order to return control.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer">The Event Hubs consumer.</param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="numberOfBatchesToRead"></param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
        /// <param name="context"></param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> to be used for iterating over messages.</returns>
        public static IAsyncEnumerable<EventData<T>> ReadMessagesFromWriterIdAsync<T>(this EventHubConsumerClient consumer, string dataSetWriterId,
            int numberOfBatchesToRead, CancellationToken cancellationToken, IIoTPlatformTestContext context = null) where T : BaseEventTypePayload
        {
            return ReadMessagesFromWriterIdAsync(consumer, dataSetWriterId, numberOfBatchesToRead, context, cancellationToken)
                        .Select(x =>
                            new EventData<T>
                            {
                                EnqueuedTime = x.enqueuedTime,
                                WriterGroupId = x.writerGroupId,
                                Payload = x.payload.ToObject<T>()
                            });
        }

        /// <summary>
        /// <para>
        ///   Reads events from all partitions of the IoT Hub Event Hubs endpoint as an asynchronous enumerable, allowing events to be iterated as they
        ///   become available on the partition, waiting as necessary should there be no events available.
        /// </para>
        /// <para>  Reading begins at the end of each partition seeing only new events as they are published.</para>
        /// <para>
        ///   Breaks up the batched messages contained in the event, and returns only messages for the provided
        ///   DataSetWriterId.
        /// </para>
        /// <para>
        ///   This enumerator may block for an indeterminate amount of time for an <c>await</c> if events are not available on the partition, requiring
        ///   cancellation via the <paramref name="cancellationToken"/> to be requested in order to return control.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer">The Event Hubs consumer.</param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="numberOfBatchesToRead"></param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
        /// <param name="context"></param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> to be used for iterating over messages.</returns>
        public static IAsyncEnumerable<PendingConditionEventData<T>> ReadConditionMessagesFromWriterIdAsync<T>(this EventHubConsumerClient consumer,
            string dataSetWriterId, int numberOfBatchesToRead, CancellationToken cancellationToken, IIoTPlatformTestContext context = null) where T : BaseEventTypePayload
        {
            return ReadMessagesFromWriterIdAsync(consumer, dataSetWriterId, numberOfBatchesToRead, context, cancellationToken)
                .Select(x =>
                    new PendingConditionEventData<T>
                    {
                        IsPayloadCompressed = x.isPayloadCompressed,
                        Payload = x.payload.ToObject<T>()
                    }
                );
        }

        /// <summary>
        /// <para>
        ///   Reads events from all partitions of the IoT Hub Event Hubs endpoint as an asynchronous enumerable, allowing events to be iterated as they
        ///   become available on the partition, waiting as necessary should there be no events available.
        /// </para>
        /// <para>  Reading begins at the end of each partition seeing only new events as they are published.</para>
        /// <para>
        ///   Breaks up the batched messages contained in the event, and returns only messages for the provided
        ///   DataSetWriterId.
        /// </para>
        /// <para>
        ///   This enumerator may block for an indeterminate amount of time for an <c>await</c> if events are not available on the partition, requiring
        ///   cancellation via the <paramref name="cancellationToken"/> to be requested in order to return control.
        /// </para>
        /// </summary>
        /// <param name="consumer">The Event Hubs consumer.</param>
        /// <param name="dataSetWriterId"></param>
        /// <param name="numberOfBatchesToRead"></param>
        /// <param name="context"></param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{JObject}"/> to be used for iterating over messages.</returns>
        public static async IAsyncEnumerable<(DateTime enqueuedTime, string writerGroupId, JObject payload, bool isPayloadCompressed)> ReadMessagesFromWriterIdAsync(this EventHubConsumerClient consumer, string dataSetWriterId,
            int numberOfBatchesToRead, IIoTPlatformTestContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var events = consumer.ReadEventsAsync(false, cancellationToken: cancellationToken);
            await foreach (var partitionEvent in events.WithCancellation(cancellationToken))
            {
                var enqueuedTime = (DateTime)partitionEvent.Data.SystemProperties[MessageSystemPropertyNames.EnqueuedTime];
                JToken json = null;
                if (!partitionEvent.Data.Properties.TryGetValue("$$ContentType", out var contentType))
                {
                    // Assert.Fail($"Missing $$ContentType property in message {partitionEvent.DeserializeJson<JToken>()}");
                    continue;
                }
                var isPayloadCompressed = (string)contentType == "application/json+gzip";
                if (isPayloadCompressed)
                {
                    var compressedPayload = Convert.FromBase64String(partitionEvent.Data.EventBody.ToString());
                    await using (var input = new MemoryStream(compressedPayload))
                    {
                        await using (var gs = new GZipStream(input, CompressionMode.Decompress))
                        {
                            using (var textReader = new StreamReader(gs))
                            {
                                json = JsonConvert.DeserializeObject<JToken>(await textReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false));
                            }
                        }
                    }
                }
                else
                {
                    json = partitionEvent.DeserializeJson<JToken>();
                }

                if (context?.OutputHelper != null)
                {
                    context.OutputHelper.WriteLine(json.ToString(Formatting.Indented));
                }

                List<dynamic> batchedMessages;
                if (json is JArray array)
                {
                    batchedMessages = array.Cast<dynamic>().ToList();
                }
                else
                {
                    batchedMessages = new List<dynamic> { json };
                }

                // Expect all messages to be the same
                var messageIds = new HashSet<string>();
                foreach (var message in batchedMessages)
                {
                    Assert.NotNull(message.MessageId.Value);
                    Assert.True(messageIds.Add(message.MessageId.Value));
                    var writerGroupId = (string)message.DataSetWriterGroup.Value;
                    Assert.NotNull(writerGroupId);
                    Assert.Equal("ua-data", message.MessageType.Value);
                    var innerMessages = (JArray)message.Messages;
                    Assert.True(innerMessages.Any(), "Json doesn't contain any messages");

                    foreach (dynamic innerMessage in innerMessages)
                    {
                        var messageWriterId = (string)innerMessage.DataSetWriterId.Value;
                        if (!messageWriterId.StartsWith(dataSetWriterId, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        // Metadata disabled, always sending version 1
                        Assert.Equal(1, innerMessage.MetaDataVersion.MajorVersion.Value);

                        yield return (enqueuedTime, writerGroupId, (JObject)innerMessage.Payload, isPayloadCompressed);
                    }
                }

                if (batchedMessages.Count > 0 && --numberOfBatchesToRead == 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns elements from an async-enumerable sequence for a given time duration.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="predicate">While condition.</param>
        /// <returns>An async-enumerable sequence that contains the elements from the input sequence for the given time period, starting when the first element is retrieved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        private static IAsyncEnumerable<TSource> TakeWhile<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, TSource, bool> predicate)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            object firstSeen = null;
            return source.TakeWhile(predicate: s =>
            {
                firstSeen ??= s;
                return predicate((TSource)firstSeen, s);
            });
        }

        /// <summary>
        /// Bypasses elements in an async-enumerable sequence as long as a number of distinct items has not been seen, and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TValue">The type of the elements to be compared for distinct values.</typeparam>
        /// <param name="source">An async-enumerable sequence to return elements from.</param>
        /// <param name="valueFunc">A function to generate the value to test for uniqueness, for each element.</param>
        /// <param name="distinctCountToReach">Number of distinct values for valueFunc output to observe until elements should be passed.</param>
        /// <param name="before">An optional action to execute when the first element is observed.</param>
        /// <param name="after">An optional action to execute when the last bypassed element is observed.</param>
        /// <returns>An async-enumerable sequence that contains the elements from the input sequence starting at the first element in the linear series at which the number of distinct values has been observed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="valueFunc"/> is null.</exception>
        private static IAsyncEnumerable<TSource> SkipUntilDistinctCountReached<TSource, TValue>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TValue> valueFunc,
            int distinctCountToReach,
            Action before = default,
            Action after = default
        )
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            _ = valueFunc ?? throw new ArgumentNullException(nameof(valueFunc));

            var seenValues = new HashSet<TValue>();
            return source.SkipWhile(m =>
            {
                if (seenValues.Count == 0)
                {
                    before?.Invoke();
                }

                seenValues.Add(valueFunc(m));
                if (seenValues.Count < distinctCountToReach)
                {
                    return true;
                }

                after?.Invoke();
                return false;
            });
        }

        /// <summary>
        /// Returns elements from an async-enumerable sequence for a given time duration,
        /// starting when one message has been published from every publishing source (PLC simulator).
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="writerGroupIdFunc">A function to extract the Data Source ID from a message payload.</param>
        /// <param name="predicate">While condition.</param>
        /// <returns>An async-enumerable sequence that contains the elements from the input sequence for the given time period, starting when the first element is retrieved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        private static IAsyncEnumerable<TSource> TakeWhile<TSource>(
            this IAsyncEnumerable<TSource> source,
            IIoTPlatformTestContext context,
            Func<TSource, string> writerGroupIdFunc,
            Func<TSource, TSource, bool> predicate
        )
        {
            // When the first message has been received for each simulator, the system is up and we
            // "let the flag fall" to start computing event rates.
            return source
                .SkipUntilDistinctCountReached(
                    writerGroupIdFunc,
                    context.PlcAciDynamicUrls.Count,
                    () => context.OutputHelper.WriteLine("Waiting for first message for PLC"),
                    () => context.OutputHelper.WriteLine("Consuming messages...")
                )
                .TakeWhile(predicate);
        }

        /// <summary>
        /// Returns elements from an async-enumerable sequence for a given time duration,
        /// starting when one message has been published from every publishing source (PLC simulator).
        /// </summary>
        /// <typeparam name="TPayload">The type of the payloads in the Publisher messages.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="predicate">A while condition.</param>
        /// <returns>An async-enumerable sequence that contains the elements from the input sequence for the given time period, starting when the first element is retrieved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static IAsyncEnumerable<EventData<TPayload>> TakeWhile<TPayload>(
            this IAsyncEnumerable<EventData<TPayload>> source,
            IIoTPlatformTestContext context,
            Func<EventData<TPayload>, EventData<TPayload>, bool> predicate
        ) where TPayload : BaseEventTypePayload
        {
            return source.TakeWhile(context, m => m.WriterGroupId, predicate);
        }

        /// <summary>
        /// Truncate a date time
        /// </summary>
        /// <param name="dateTime">Date time top truncate</param>
        /// <param name="timeSpan">Time span</param>
        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
            {
                return dateTime;
            }

            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
            {
                return dateTime;
            }

            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        /// <summary>
        /// Initialize DeviceServiceClient from IoT Hub connection string.
        /// </summary>
        /// <param name="iotHubConnectionString"></param>
        /// <param name="transportType"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static ServiceClient DeviceServiceClient(
            string iotHubConnectionString,
            TransportType transportType = TransportType.Amqp_WebSocket_Only
        )
        {
            if (string.IsNullOrWhiteSpace(iotHubConnectionString))
            {
                throw new ArgumentNullException(nameof(iotHubConnectionString));
            }

            return ServiceClient.CreateFromConnectionString(
                iotHubConnectionString,
                transportType
            );
        }

        /// <summary>
        /// Restart module
        /// </summary>
        /// <param name="context"></param>
        /// <param name="moduleName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task RestartAsync(IIoTPlatformTestContext context, string moduleName,
            CancellationToken ct)
        {
            using var client = TestHelper.DeviceServiceClient(context.IoTHubConfig.IoTHubConnectionString,
                    TransportType.Amqp_WebSocket_Only);
            var method = new CloudToDeviceMethod("Shutdown");
            method.SetPayloadJson("false");
            try
            {
                await client.InvokeDeviceMethodAsync(context.DeviceId, moduleName, method, ct);
            }
            catch { } // Expected, since device will have disconnected now
        }

        /// <summary>
        /// Prints the exception message and stacktrace for exception (and all inner exceptions) in test output
        /// </summary>
        /// <param name="e">Exception to be printed</param>
        /// <param name="outputHelper">XUnit Test OutputHelper instance or null (no print in this case)</param>
        private static void PrettyPrintException(Exception e, ITestOutputHelper outputHelper)
        {
            var exception = e;
            while (exception != null)
            {
                outputHelper.WriteLine(exception.Message);
                outputHelper.WriteLine(exception.StackTrace);
                outputHelper.WriteLine("");
                exception = exception.InnerException;
            }
        }

        /// <summary>
        /// Call a direct method
        /// </summary>
        /// <param name="serviceClient">Device service client</param>
        /// <param name="deviceId">Device Id</param>
        /// <param name="moduleId">Module Id</param>
        /// <param name="parameters">Method parameter </param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task<MethodResultModel> CallMethodAsync(ServiceClient serviceClient, string deviceId, string moduleId,
            MethodParameterModel parameters, IIoTPlatformTestContext context, CancellationToken ct)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    var i = 0;// Retry twice to call with error 500
                    while (true)
                    {
                        var methodInfo = new CloudToDeviceMethod(parameters.Name);
                        methodInfo.SetPayloadJson(parameters.JsonPayload);
                        var result = await (string.IsNullOrEmpty(moduleId) ?
                             serviceClient.InvokeDeviceMethodAsync(deviceId, methodInfo, ct) :
                             serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInfo, ct)).ConfigureAwait(false);
                        context.OutputHelper.WriteLine($"Called method {parameters.Name}.");
                        var methodCallResult = new MethodResultModel(result.GetPayloadAsJson(), result.Status);
                        if (methodCallResult.Status >= 500 && ++i < 3)
                        {
                            context.OutputHelper.WriteLine($"Got internal error {methodCallResult.Status} ({methodCallResult.JsonPayload}), trying again to call publisher after delay...");
                            await Task.Delay(2000, ct).ConfigureAwait(false);
                            continue;
                        }
                        return methodCallResult;
                    }
                }
                catch (DeviceNotFoundException de) when (attempt < 60)
                {
                    context.OutputHelper.WriteLine($"Failed to call method {parameters.Name} with {parameters.JsonPayload} due to {de.Message}");
                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    context.OutputHelper.WriteLine($"Failed to call method {parameters.Name} with {parameters.JsonPayload}");
                    if (e.Message.Contains("The operation failed because the requested device isn't online", StringComparison.Ordinal) && attempt < 60)
                    {
                        context.OutputHelper.WriteLine("Device is not online, trying again to call device after delay...");
                        await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                        continue;
                    }
                    PrettyPrintException(e, context.OutputHelper);
                    throw;
                }
            }
        }

        private static readonly JsonSerializer kSerializer = new();
    }
}
