// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests
{
    using Azure.Core;
    using Azure.Identity;
    using Azure.Messaging.EventHubs.Consumer;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Rest;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using OpcPublisherAEE2ETests.Config;
    using Renci.SshNet;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
    using Xunit.Sdk;

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
            context.OutputHelper?.WriteLine("Write published_nodes.json to IoT Edge");
            context.OutputHelper?.WriteLine(json);
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
                    await CreateFolderOnEdgeVMAsync(TestConstants.PublishedNodesFolder, context).ConfigureAwait(false);
            		using var scpClient = await CreateScpClientAndConnectAsync(context).ConfigureAwait(false);
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    scpClient.Upload(stream, TestConstants.PublishedNodesFullName);

                    if (context.IoTEdgeConfig.NestedEdgeFlag == "Enable")
                    {
                        using var sshCient = await CreateSshClientAndConnectAsync(context).ConfigureAwait(false);
                        foreach (var edge in context.IoTEdgeConfig.NestedEdgeSshConnections)
                        {
                            if (!string.IsNullOrEmpty(edge))
                            {
                                // Copy file to the edge vm
                                var command = $"scp -oStrictHostKeyChecking=no {TestConstants.PublishedNodesFullName} {edge}:{TestConstants.PublishedNodesFilename}";
                                sshCient.RunCommand(command);
                                // Move file to the target folder with sudo permissions
                                command = $"ssh -oStrictHostKeyChecking=no {edge} 'sudo mv {TestConstants.PublishedNodesFilename} {TestConstants.PublishedNodesFullName}'";
                                sshCient.RunCommand(command);
                            }
                        }
                    }
                    return;
                }
                catch (Exception ex) when (attempt < 60)
                {
                    context.OutputHelper?.WriteLine("Failed to write published nodes file to host {0} with username {1} ({2})",
                        context.SshConfig.Host,
                        context.SshConfig.Username, ex.Message);
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Clean published nodes JSON files.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task CleanPublishedNodesJsonFilesAsync(IIoTPlatformTestContext context)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    // Make sure directories exist.
                    using (var sshCient = await CreateSshClientAndConnectAsync(context).ConfigureAwait(false))
                    {
                        sshCient.RunCommand($"[ ! -d {TestConstants.PublishedNodesFolder} ]" +
                            $" && sudo mkdir -m 777 -p {TestConstants.PublishedNodesFolder}");
                    }
                    break;
                }
                catch (Exception ex) when (attempt < 60)
                {
                    context.OutputHelper?.WriteLine("Failed to create folder on host {0} with username {1} ({2})",
                        context.SshConfig.Host,
                        context.SshConfig.Username, ex.Message);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }

            await PublishNodesAsync("[]", context).ConfigureAwait(false);
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
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static async Task<SshClient> CreateSshClientAndConnectAsync(IIoTPlatformTestContext context)
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
                        client.Connect();
                        return client;
                    }
                    catch (SocketException) when (++connectAttempt < 5)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                context.OutputHelper?.WriteLine("Failed to open ssh connection to host {0} with username {1} ({2})",
                    context.SshConfig.Host,
                    context.SshConfig.Username, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Create a new ScpClient based on SshConfig and directly connects to host
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static async Task<ScpClient> CreateScpClientAndConnectAsync(IIoTPlatformTestContext context)
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
                        client.Connect();
                        return client;
                    }
                    catch (SocketException) when (++connectAttempt < 5)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                context.OutputHelper?.WriteLine("Failed to open scp connection to host {0} with username {1} ({2})",
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
        public static async Task DeleteFileOnEdgeVMAsync(string fileName, IIoTPlatformTestContext context)
        {
            var isSuccessful = false;
            using var client = await CreateSshClientAndConnectAsync(context).ConfigureAwait(false);

            var terminal = client.RunCommand("rm " + fileName);

            if (string.IsNullOrEmpty(terminal.Error) ||
                terminal.Error.Contains("no such file", StringComparison.OrdinalIgnoreCase))
            {
                isSuccessful = true;
            }
            Assert.True(isSuccessful, "Delete file was not successful");

            if (context.IoTEdgeConfig.NestedEdgeFlag == "Enable")
            {
                using var sshCient = await CreateSshClientAndConnectAsync(context).ConfigureAwait(false);
                foreach (var edge in context.IoTEdgeConfig.NestedEdgeSshConnections)
                {
                    if (!string.IsNullOrEmpty(edge))
                    {
                        var command = $"ssh -oStrictHostKeyChecking=no {edge} 'sudo rm {fileName}'";
                        sshCient.RunCommand(command);
                    }
                }
            }
        }

        /// <summary>
        /// Create a folder on Edge VM (if not exists)
        /// </summary>
        /// <param name="folderPath">Name of the folder to create.</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        private static async Task CreateFolderOnEdgeVMAsync(string folderPath, IIoTPlatformTestContext context)
        {
            Assert.False(string.IsNullOrWhiteSpace(folderPath));

            var isSuccessful = false;
            using var client = await CreateSshClientAndConnectAsync(context).ConfigureAwait(false);

            var terminal = client.RunCommand("sudo mkdir " + folderPath + ";cd " + folderPath + "; sudo chmod 777 " + folderPath);

            if (string.IsNullOrEmpty(terminal.Error) || terminal.Error.Contains("File exists", StringComparison.Ordinal))
            {
                isSuccessful = true;
            }

            Assert.True(isSuccessful, "Folder creation was not successful");
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
        public static async Task CreateSimulationContainerAsync(IIoTPlatformTestContext context, List<string> commandLine, CancellationToken cancellationToken, string fileToUpload = null, int numInstances = 1)
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
                            context.OpcPlcConfig.ResourceGroupName,
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
            var cloudStorageAccount = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
            var cloudFileClient = cloudStorageAccount.CreateCloudFileClient();
            var share = cloudFileClient.GetShareReference(TestConstants.OpcSimulation.FileShareName);
            var directory = share.GetRootDirectoryReference();

            Assert.False(fileName.Contains('\\', StringComparison.Ordinal), "\\ can't be used for file path");

            // if fileName contains '/' we will extract the filename
            string onlyFileName;
            if (fileName.Contains('/', StringComparison.Ordinal))
            {
                onlyFileName = fileName.Substring(fileName.LastIndexOf('/') + 1);
            }
            else
            {
                onlyFileName = fileName;
            }

            var cf = directory.GetFileReference(onlyFileName);
            await cf.UploadFromFileAsync(fileName).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a container group
        /// </summary>
        /// <param name="azure">Azure context</param>
        /// <param name="resourceGroupName">Resource group name</param>
        /// <param name="containerGroupName">Container group name</param>
        /// <param name="containerImage">Container image</param>
        /// <param name="executable">Starting command line</param>
        /// <param name="commandLine">Additional command line options</param>
        /// <param name="fileShareName">File share name</param>
        /// <param name="storageAccountName">Storage account name</param>
        /// <param name="storageAccountKey">Storage account key</param>
        private static async Task<string> CreateContainerGroupAsync(IAzure azure,
                                      string resourceGroupName,
                                      string containerGroupName,
                                      string containerImage,
                                      string executable,
                                      string[] commandLine,
                                      string fileShareName,
                                      string storageAccountName,
                                      string storageAccountKey)
        {
            IResourceGroup resGroup = await azure.ResourceGroups.GetByNameAsync(resourceGroupName).ConfigureAwait(false);
            Region azureRegion = resGroup.Region;

            var containerGroup = await azure.ContainerGroups.Define(containerGroupName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(resourceGroupName)
                .WithLinux()
                .WithPublicImageRegistryOnly()
                .DefineVolume("share")
                    .WithExistingReadWriteAzureFileShare(fileShareName)
                    .WithStorageAccountName(storageAccountName)
                    .WithStorageAccountKey(storageAccountKey)
                    .Attach()
                .DefineContainerInstance(containerGroupName)
                    .WithImage(containerImage)
                    .WithExternalTcpPort(50000)
                    .WithCpuCoreCount(0.5)
                    .WithMemorySizeInGB(0.5)
                    .WithVolumeMountSetting("share", "/app/files")
                    .WithStartingCommandLine(executable, commandLine)
                    .Attach()
                .WithDnsPrefix(containerGroupName)
                .CreateAsync().ConfigureAwait(false);

            return containerGroup.Fqdn;
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
            await Task.WhenAll(
            context.PlcAciDynamicUrls
                .Select(url => url.Split(".")[0])
                .Select(n => context.AzureContext.ContainerGroups.DeleteByResourceGroupAsync(context.OpcPlcConfig.ResourceGroupName, n))
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Get an azure context
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async static Task<IAzure> GetAzureContextAsync(IIoTPlatformTestContext context, CancellationToken cancellationToken)
        {
            if (context.AzureContext != null)
            {
                return context.AzureContext;
            }

            context.OutputHelper.WriteLine($"TenantId: {context.OpcPlcConfig.TenantId}");
            context.OutputHelper.WriteLine($"AZURE_CLIENT_ID: {Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")}");
            context.OutputHelper.WriteLine($"AZURE_CLIENT_SECRET: {Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")}");
            context.OutputHelper.WriteLine($"AZURE_TENANT_ID: {Environment.GetEnvironmentVariable("AZURE_TENANT_ID")}");

            var options = new DefaultAzureCredentialOptions
            {
                TenantId = context.OpcPlcConfig.TenantId
            };
            //options.AdditionallyAllowedTenants.Add("*");

            var defaultAzureCredential = new DefaultAzureCredential(options);
            var accessToken = await defaultAzureCredential.GetTokenAsync(new TokenRequestContext(
                new[] { "https://management.azure.com//.default" }, tenantId: context.OpcPlcConfig.TenantId), cancellationToken).ConfigureAwait(false);

            context.OutputHelper.WriteLine($"Received Token {accessToken.Token}");

            var tokenCredentials = new TokenCredentials(accessToken.Token);
            var azureCredentials = new AzureCredentials(tokenCredentials, tokenCredentials, context.OpcPlcConfig.TenantId,
                AzureEnvironment.AzureGlobalCloud);

            IAzure azure;

            if (string.IsNullOrEmpty(context.OpcPlcConfig.SubscriptionId))
            {
                azure = await Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(azureCredentials)
                    .WithDefaultSubscriptionAsync().ConfigureAwait(false);
            }
            else
            {
                azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(azureCredentials)
                    .WithSubscription(context.OpcPlcConfig.SubscriptionId);
            }

            context.AzureContext = azure;

            var testingSuffix = (await azure.ResourceGroups.GetByNameAsync(context.OpcPlcConfig.ResourceGroupName,
                cancellationToken).ConfigureAwait(false)).Tags[TestConstants.OpcSimulation.TestingResourcesSuffixName];
            context.TestingSuffix = testingSuffix;
            context.AzureStorageName = TestConstants.OpcSimulation.AzureStorageNameWithoutSuffix + testingSuffix;

            var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(context.OpcPlcConfig.ResourceGroupName,
                context.AzureStorageName, cancellationToken).ConfigureAwait(false);
            context.AzureStorageKey = (await storageAccount.GetKeysAsync(cancellationToken).ConfigureAwait(false))[0].Value;

            var firstAciIpAddress = context.OpcPlcConfig.Urls.Split(";")[0];
            var containerGroups = (await azure.ContainerGroups.ListByResourceGroupAsync(context.OpcPlcConfig.ResourceGroupName,
                cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();
            var containerGroup = (await azure.ContainerGroups.ListByResourceGroupAsync(context.OpcPlcConfig.ResourceGroupName,
                cancellationToken: cancellationToken).ConfigureAwait(false))
                .First(g => g.IPAddress == firstAciIpAddress);
            context.PLCImage = containerGroup.Containers.First().Value.Image;

            return azure;
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
        public static EventHubConsumerClient GetEventHubConsumerClient(this IIoTHubConfig config)
        {
            return new EventHubConsumerClient(
                TestConstants.TestConsumerGroupName,
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
            => ReadMessagesFromWriterIdAsync(consumer, dataSetWriterId, numberOfBatchesToRead, context, cancellationToken)
                .Select(x =>
                    new EventData<T>
                    {
                        EnqueuedTime = x.enqueuedTime,
                        WriterGroupId = x.writerGroupId,
                        Payload = x.payload.ToObject<T>()
                    });

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
                bool isPayloadCompressed = (string)contentType == "application/json+gzip";
                if (isPayloadCompressed)
                {
                    var compressedPayload = Convert.FromBase64String(partitionEvent.Data.EventBody.ToString());
                    using (var input = new MemoryStream(compressedPayload))
                    {
                        using (var gs = new GZipStream(input, CompressionMode.Decompress))
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
                foreach (dynamic message in batchedMessages)
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
                if (firstSeen is null)
                {
                    firstSeen = s;
                }
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
                    () => context.OutputHelper?.WriteLine("Waiting for first message for PLC"),
                    () => context.OutputHelper?.WriteLine("Consuming messages...")
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
            Microsoft.Azure.Devices.TransportType transportType = Microsoft.Azure.Devices.TransportType.Amqp_WebSocket_Only
        )
        {
            ServiceClient iotHubClient;

            if (string.IsNullOrWhiteSpace(iotHubConnectionString))
            {
                throw new ArgumentNullException(nameof(iotHubConnectionString));
            }

            return iotHubClient = ServiceClient.CreateFromConnectionString(
                iotHubConnectionString,
                transportType
            );
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
            var attempt = 0;
            while (true)
            {
                try
                {
                    var methodInfo = new CloudToDeviceMethod(parameters.Name);
                    methodInfo.SetPayloadJson(parameters.JsonPayload);
                    var result = await (string.IsNullOrEmpty(moduleId) ?
                         serviceClient.InvokeDeviceMethodAsync(deviceId, methodInfo, ct) :
                         serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInfo, ct)).ConfigureAwait(false);
                    context.OutputHelper.WriteLine($"Called method {parameters.Name}.");
                    return new MethodResultModel(result.GetPayloadAsJson(), result.Status);
                }
                catch (Exception e)
                {
                    context.OutputHelper.WriteLine($"Failed to call method {parameters.Name} with {parameters.JsonPayload}");
                    if (e.Message.Contains("The operation failed because the requested device isn't online", StringComparison.Ordinal) && ++attempt < 60)
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

        private static readonly JsonSerializer kSerializer = new JsonSerializer();
    }
}
