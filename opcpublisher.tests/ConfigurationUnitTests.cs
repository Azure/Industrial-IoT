
using System.Collections.Generic;
using Xunit;

namespace OpcPublisher
{
    using Microsoft.Azure.Devices.Client;
    using Moq;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Abstractions;
    using static OpcApplicationConfiguration;
    using static Program;
    using static PublisherNodeConfiguration;

    public sealed class ConfigurationUnitTests : IDisposable, IClassFixture<PlcOpcUaServerFixture>, IClassFixture<TestDirectoriesFixture>, IClassFixture<OpcPublisherFixture>
    {
        public ConfigurationUnitTests(ITestOutputHelper output)
        {
            // xunit output
            _output = output;

            // init configuration objects
            PublisherNodeConfiguration.Init();
            PublisherTelemetryConfiguration.Init();
            PublisherTelemetryConfiguration.ReadConfigAsync().Wait();

            // mock IoTHub communication
            var hubMockBase = new Mock<HubCommunicationBase>();
            var hubMock = hubMockBase.As<IHubCommunication>();
            hubMock.CallBase = true;
            MessageData json = new MessageData();
            hubMock.Setup(hubComm =>
                    hubComm.Enqueue(json)
            );
            Hub = hubMock.Object;
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                PublisherNodeConfiguration.Deinit();
                PublisherTelemetryConfiguration.Deinit();
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            // do cleanup
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Test reading different configuration files.
        /// </summary>
#pragma warning disable xUnit1026
        [Theory]
        [MemberData(nameof(PnPlcSimple))]
        public async void ReadConfigAsync(string testFilename, int configuredSessions,
            int configuredSubscriptions, int configuredMonitoredItems)
#pragma warning restore xUnit1026
        {
            //string methodName = "ReadConfigAsync";
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/publishernodeconfiguration/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfigurationFilename = fqTempFilename;
            Assert.True(File.Exists(PublisherNodeConfigurationFilename));
            Assert.True(await PublisherNodeConfiguration.ReadConfigAsync().ConfigureAwait(false));

        }

        /// <summary>
        /// Test reading different configuration files and creating the correct internal data structures.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcSimple))]
        public async void CreateOpcPublishingDataAsync(string testFilename, int configuredSessions,
            int configuredSubscriptions, int configuredMonitoredItems)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/publishernodeconfiguration/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfigurationFilename));
            Assert.True(await PublisherNodeConfiguration.ReadConfigAsync().ConfigureAwait(false));
            Assert.True(await PublisherNodeConfiguration.CreateOpcPublishingDataAsync().ConfigureAwait(false));
            await Task.Yield();
            Assert.True(OpcSessions.Count == configuredSessions, "wrong # of sessions");
            Assert.True(NumberOfOpcSessionsConfigured == configuredSessions, "wrong # of sessions");
            Assert.True(NumberOfOpcSubscriptionsConfigured == configuredSubscriptions, "wrong # of subscriptions");
            Assert.True(NumberOfOpcMonitoredItemsConfigured == configuredMonitoredItems, "wrong # of monitored items");
            _output.WriteLine($"sessions configured {NumberOfOpcSessionsConfigured}, connected {NumberOfOpcSessionsConnected}");
            _output.WriteLine($"subscriptions configured {NumberOfOpcSubscriptionsConfigured}, connected {NumberOfOpcSubscriptionsConnected}");
            _output.WriteLine($"items configured {NumberOfOpcMonitoredItemsConfigured}, monitored {NumberOfOpcMonitoredItemsMonitored}, toRemove {NumberOfOpcMonitoredItemsToRemove}");
        }

        /// <summary>
        /// Test that OpcPublishingInterval setting is not added when default setting is different.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcOpcPublishingIntervalNone))]
        public async void OpcPublishingIntervalSettingIsDefaultPublishingIntervalButNotPersisted(string testFilename, int configuredSessions,
                            int configuredSubscriptions, int configuredMonitoredItems)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcpublishinginterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 3000;
            _output.WriteLine($"now testing: {PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfigurationFilename));
            Assert.True(await PublisherNodeConfiguration.ReadConfigAsync().ConfigureAwait(false));
            Assert.True(await PublisherNodeConfiguration.CreateOpcPublishingDataAsync().ConfigureAwait(false));
            Assert.True(OpcSessions.Count == configuredSessions, "wrong # of sessions");
            Assert.True(NumberOfOpcSessionsConfigured == configuredSessions, "wrong # of sessions");
            Assert.True(NumberOfOpcSubscriptionsConfigured == configuredSubscriptions, "wrong # of subscriptions");
            Assert.True(NumberOfOpcMonitoredItemsConfigured == configuredMonitoredItems, "wrong # of monitored items");
            _output.WriteLine($"sessions configured {NumberOfOpcSessionsConfigured}, connected {NumberOfOpcSessionsConnected}");
            _output.WriteLine($"subscriptions configured {NumberOfOpcSubscriptionsConfigured}, connected {NumberOfOpcSubscriptionsConnected}");
            _output.WriteLine($"items configured {NumberOfOpcMonitoredItemsConfigured}, monitored {NumberOfOpcMonitoredItemsMonitored}, toRemove {NumberOfOpcMonitoredItemsToRemove}");
            Assert.True(OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 3000);
            await UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
            _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
            _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfigurationFilename));
            Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == null);
        }


        /// <summary>
        /// Test that OpcPublishingInterval setting is not removed when default setting is different.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcOpcPublishingInterval2000))]
        public async void OpcPublishingIntervalSettingWithDifferentDefaultPublishingInterval(string testFilename, int configuredSessions,
                            int configuredSubscriptions, int configuredMonitoredItems)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcpublishinginterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 0;
            _output.WriteLine($"now testing: {PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfigurationFilename));
            Assert.True(await PublisherNodeConfiguration.ReadConfigAsync().ConfigureAwait(false));
            Assert.True(await PublisherNodeConfiguration.CreateOpcPublishingDataAsync().ConfigureAwait(false));
            Assert.True(OpcSessions.Count == configuredSessions, "wrong # of sessions");
            Assert.True(NumberOfOpcSessionsConfigured == configuredSessions, "wrong # of sessions");
            Assert.True(NumberOfOpcSubscriptionsConfigured == configuredSubscriptions, "wrong # of subscriptions");
            Assert.True(NumberOfOpcMonitoredItemsConfigured == configuredMonitoredItems, "wrong # of monitored items");
            _output.WriteLine($"sessions configured {NumberOfOpcSessionsConfigured}, connected {NumberOfOpcSessionsConnected}");
            _output.WriteLine($"subscriptions configured {NumberOfOpcSubscriptionsConfigured}, connected {NumberOfOpcSubscriptionsConnected}");
            _output.WriteLine($"items configured {NumberOfOpcMonitoredItemsConfigured}, monitored {NumberOfOpcMonitoredItemsMonitored}, toRemove {NumberOfOpcMonitoredItemsToRemove}");
            Assert.True(OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 2000);
            await UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
            _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
            _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfigurationFilename));
            Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == 2000);
        }

        /// <summary>
        /// Test that OpcPublishingInterval setting is not removed when default setting is the same.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcOpcPublishingInterval2000))]
        public async void OpcPublishingIntervalSettingWithSimilarDefaultPublishingInterval(string testFilename, int configuredSessions,
                            int configuredSubscriptions, int configuredMonitoredItems)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcpublishinginterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 2000;
            _output.WriteLine($"now testing: {PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfigurationFilename));
            Assert.True(await PublisherNodeConfiguration.ReadConfigAsync().ConfigureAwait(false));
            Assert.True(await PublisherNodeConfiguration.CreateOpcPublishingDataAsync().ConfigureAwait(false));
            Assert.True(OpcSessions.Count == configuredSessions, "wrong # of sessions");
            Assert.True(NumberOfOpcSessionsConfigured == configuredSessions, "wrong # of sessions");
            Assert.True(NumberOfOpcSubscriptionsConfigured == configuredSubscriptions, "wrong # of subscriptions");
            Assert.True(NumberOfOpcMonitoredItemsConfigured == configuredMonitoredItems, "wrong # of monitored items");
            _output.WriteLine($"sessions configured {NumberOfOpcSessionsConfigured}, connected {NumberOfOpcSessionsConnected}");
            _output.WriteLine($"subscriptions configured {NumberOfOpcSubscriptionsConfigured}, connected {NumberOfOpcSubscriptionsConnected}");
            _output.WriteLine($"items configured {NumberOfOpcMonitoredItemsConfigured}, monitored {NumberOfOpcMonitoredItemsMonitored}, toRemove {NumberOfOpcMonitoredItemsToRemove}");
            Assert.True(OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 2000);
            await UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
            _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
            _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfigurationFilename));
            Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == 2000);
        }

        /// <summary>
        /// Test that OpcPublishingInterval setting is persisted when configured via method and is different than default.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcEmptyAndPayload))]
        public async void OpcPublishingIntervalSettingIsPersistentWhenConfiguredViaMethodAndDifferentThanDefault(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcpublishinginterval/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcpublishinginterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 3000;
            _output.WriteLine($"now testing: {PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfigurationFilename));
            Assert.True(await PublisherNodeConfiguration.ReadConfigAsync().ConfigureAwait(false));
            Assert.True(await PublisherNodeConfiguration.CreateOpcPublishingDataAsync().ConfigureAwait(false));
            Assert.True(OpcSessions.Count == 0, "wrong # of sessions");
            Assert.True(NumberOfOpcSessionsConfigured == 0, "wrong # of sessions");
            Assert.True(NumberOfOpcSubscriptionsConfigured == 0, "wrong # of subscriptions");
            Assert.True(NumberOfOpcMonitoredItemsConfigured == 0, "wrong # of monitored items");
            _output.WriteLine($"sessions configured {NumberOfOpcSessionsConfigured}, connected {NumberOfOpcSessionsConnected}");
            _output.WriteLine($"subscriptions configured {NumberOfOpcSubscriptionsConfigured}, connected {NumberOfOpcSubscriptionsConnected}");
            _output.WriteLine($"items configured {NumberOfOpcMonitoredItemsConfigured}, monitored {NumberOfOpcMonitoredItemsMonitored}, toRemove {NumberOfOpcMonitoredItemsToRemove}");
            await UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
            _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
            _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfigurationFilename));
            Assert.True(_configurationFileEntries.Count == 0);
            MethodRequest methodRequest = new MethodRequest("PublishNodes", File.ReadAllBytes(fqPayloadFilename));
            await Hub.HandlePublishNodesMethodAsync(methodRequest, null).ConfigureAwait(false);
            await Task.Yield();
            Assert.True(OpcSessions.Count == 1);
            Assert.True(OpcSessions[0].OpcSubscriptions.Count == 1);
            Assert.True(OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 2000);
            await UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
            _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
            _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfigurationFilename));
            Assert.True(OpcSessions.Count == 1, "wrong # of sessions");
            Assert.True(NumberOfOpcSessionsConfigured == 1, "wrong # of sessions");
            Assert.True(NumberOfOpcSubscriptionsConfigured == 1, "wrong # of subscriptions");
            Assert.True(NumberOfOpcMonitoredItemsConfigured == 1, "wrong # of monitored items");
            _output.WriteLine($"sessions configured {NumberOfOpcSessionsConfigured}, connected {NumberOfOpcSessionsConnected}");
            _output.WriteLine($"subscriptions configured {NumberOfOpcSubscriptionsConfigured}, connected {NumberOfOpcSubscriptionsConnected}");
            _output.WriteLine($"items configured {NumberOfOpcMonitoredItemsConfigured}, monitored {NumberOfOpcMonitoredItemsMonitored}, toRemove {NumberOfOpcMonitoredItemsToRemove}");
            Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == 2000);
        }

        /// <summary>
        /// Test that OpcPublishingInterval setting is persisted when configured via method and is different than default.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcEmptyAndPayload))]
        public async void OpcPublishingIntervalSettingIsPersistentWhenConfiguredViaMethodAndSameThanDefault(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcpublishinginterval/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcpublishinginterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 2000;
            _output.WriteLine($"now testing: {PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfigurationFilename));
            Assert.True(await PublisherNodeConfiguration.ReadConfigAsync().ConfigureAwait(false));
            Assert.True(await PublisherNodeConfiguration.CreateOpcPublishingDataAsync().ConfigureAwait(false));
            Assert.True(OpcSessions.Count == 0, "wrong # of sessions");
            Assert.True(NumberOfOpcSessionsConfigured == 0, "wrong # of sessions");
            Assert.True(NumberOfOpcSubscriptionsConfigured == 0, "wrong # of subscriptions");
            Assert.True(NumberOfOpcMonitoredItemsConfigured == 0, "wrong # of monitored items");
            _output.WriteLine($"sessions configured {NumberOfOpcSessionsConfigured}, connected {NumberOfOpcSessionsConnected}");
            _output.WriteLine($"subscriptions configured {NumberOfOpcSubscriptionsConfigured}, connected {NumberOfOpcSubscriptionsConnected}");
            _output.WriteLine($"items configured {NumberOfOpcMonitoredItemsConfigured}, monitored {NumberOfOpcMonitoredItemsMonitored}, toRemove {NumberOfOpcMonitoredItemsToRemove}");
            await UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
            _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
            _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfigurationFilename));
            Assert.True(_configurationFileEntries.Count == 0);
            MethodRequest methodRequest = new MethodRequest("PublishNodes", File.ReadAllBytes(fqPayloadFilename));
            await Hub.HandlePublishNodesMethodAsync(methodRequest, null).ConfigureAwait(false);
            await Task.Yield();
            Assert.True(OpcSessions.Count == 1);
            Assert.True(OpcSessions[0].OpcSubscriptions.Count == 1);
            Assert.True(OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 2000);
            await UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
            _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
            _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfigurationFilename));
            Assert.True(OpcSessions.Count == 1, "wrong # of sessions");
            Assert.True(NumberOfOpcSessionsConfigured == 1, "wrong # of sessions");
            Assert.True(NumberOfOpcSubscriptionsConfigured == 1, "wrong # of subscriptions");
            Assert.True(NumberOfOpcMonitoredItemsConfigured == 1, "wrong # of monitored items");
            _output.WriteLine($"sessions configured {NumberOfOpcSessionsConfigured}, connected {NumberOfOpcSessionsConnected}");
            _output.WriteLine($"subscriptions configured {NumberOfOpcSubscriptionsConfigured}, connected {NumberOfOpcSubscriptionsConnected}");
            _output.WriteLine($"items configured {NumberOfOpcMonitoredItemsConfigured}, monitored {NumberOfOpcMonitoredItemsMonitored}, toRemove {NumberOfOpcMonitoredItemsToRemove}");
            Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == 2000);
        }



        public static IEnumerable<object[]> PnPlcSimple =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_simple_nid.json"),
                    // # of configured sessions
                    1,
                    // # of configured subscriptions
                    1,
                    // # of configured monitored items
                    2
                },
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_simple_nid_enid.json"),
                    // # of configured sessions
                    1,
                    // # of configured subscriptions
                    1,
                    // # of configured monitored items
                    3
                },
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_simple_nid_enid_id.json"),
                    // # of configured sessions
                    1,
                    // # of configured subscriptions
                    1,
                    // # of configured monitored items
                    6
                },
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_simple_enid.json"),
                    // # of configured sessions
                    1,
                    // # of configured subscriptions
                    1,
                    // # of configured monitored items
                    1
                },
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_simple_enid_id.json"),
                    // # of configured sessions
                    1,
                    // # of configured subscriptions
                    1,
                    // # of configured monitored items
                    4
                },
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_simple_id.json"),
                    // # of configured sessions
                    1,
                    // # of configured subscriptions
                    1,
                    // # of configured monitored items
                    3
                },
            };
        public static IEnumerable<object[]> PnPlcOpcPublishingIntervalNone =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_publishing_interval_none.json"),
                    // # of configured sessions
                    1,
                    // # of configured subscriptions
                    1,
                    // # of configured monitored items
                    1
                },
            };

        public static IEnumerable<object[]> PnPlcOpcPublishingInterval2000 =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_publishing_interval_2000.json"),
                    // # of configured sessions
                    1,
                    // # of configured subscriptions
                    1,
                    // # of configured monitored items
                    1
                },
            };

        public static IEnumerable<object[]> PnPlcEmpty =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // # of configured sessions
                    0,
                    // # of configured subscriptions
                    0,
                    // # of configured monitored items
                    0
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayload =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_publishing_interval_2000.json"),
                },
            };

        private readonly ITestOutputHelper _output;
        private static List<PublisherConfigurationFileEntryLegacyModel> _configurationFileEntries;
    }
}
