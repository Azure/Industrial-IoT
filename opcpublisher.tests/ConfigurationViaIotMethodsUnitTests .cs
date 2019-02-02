using System.Collections.Generic;
using Xunit;

namespace OpcPublisher
{
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Xunit.Abstractions;
    using static OpcApplicationConfiguration;
    using static Program;

    [Collection("Need PLC and app config")]
    public sealed class ConfigurationViaIotMethodUnitTests : IDisposable
    {
        public ConfigurationViaIotMethodUnitTests(ITestOutputHelper output)
        {
            // xunit output
            _output = output;

            // init configuration objects
            TelemetryConfiguration = PublisherTelemetryConfiguration.Instance;
            Diag = PublisherDiagnostics.Instance;
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        void Dispose(bool disposing)
        {
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
            // do cleanup
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Test that OpcPublishingInterval setting is not persisted when not configured via method.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcEmptyAndPayloadNoPublishingInterval))]
        public async void OpcPublishingIntervalSettingNotConfiguredAndIsNotPersisted(string testFilename, string payloadFilename)
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
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 2000;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            try
            {
                Hub = IotHubCommunication.Instance;
                NodeConfiguration = PublisherNodeConfiguration.Instance;
                Assert.True(NodeConfiguration.OpcSessions.Count == 0, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == 0, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == 0, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == 0, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
                _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
                Assert.True(_configurationFileEntries.Count == 0);
                MethodRequest methodRequest = new MethodRequest("PublishNodes", File.ReadAllBytes(fqPayloadFilename));
                await Hub.HandlePublishNodesMethodAsync(methodRequest, null).ConfigureAwait(false);
                await Task.Yield();
                Assert.True(NodeConfiguration.OpcSessions.Count == 1);
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions.Count == 1);
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 2000);
                await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
                _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
                Assert.True(NodeConfiguration.OpcSessions.Count == 1, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == 1, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == 1, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == 1, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == null);
            }
            finally
            {
                NodeConfiguration.Dispose();
                NodeConfiguration = null;
                Hub.Dispose();
                Hub = null;
            }
        }

        /// <summary>
        /// Test that OpcPublishingInterval setting is persisted when configured via method and is different as default.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcEmptyAndPayloadPublishingInterval2000))]
        public async void OpcPublishingIntervalSettingAndDifferentAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
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
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 3000;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            try
            {
                Hub = IotHubCommunication.Instance;
                NodeConfiguration = PublisherNodeConfiguration.Instance;
                Assert.True(NodeConfiguration.OpcSessions.Count == 0, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == 0, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == 0, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == 0, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
                _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
                Assert.True(_configurationFileEntries.Count == 0);
                MethodRequest methodRequest = new MethodRequest("PublishNodes", File.ReadAllBytes(fqPayloadFilename));
                await Hub.HandlePublishNodesMethodAsync(methodRequest, null).ConfigureAwait(false);
                Assert.True(NodeConfiguration.OpcSessions.Count == 1);
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions.Count == 1);
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 2000);
                await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
                _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
                Assert.True(NodeConfiguration.OpcSessions.Count == 1, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == 1, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == 1, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == 1, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == 2000);
            }
            finally
            {
                NodeConfiguration.Dispose();
                NodeConfiguration = null;
                Hub.Dispose();
                Hub = null;
            }
        }

        /// <summary>
        /// Test that OpcPublishingInterval setting is persisted when configured via method and is same as default.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcEmptyAndPayloadPublishingInterval2000))]
        public async void OpcPublishingIntervalSettingAndSameAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
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
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 2000;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            try
            {
                Hub = IotHubCommunication.Instance;
                NodeConfiguration = PublisherNodeConfiguration.Instance;
                Assert.True(NodeConfiguration.OpcSessions.Count == 0, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == 0, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == 0, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == 0, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
                _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
                Assert.True(_configurationFileEntries.Count == 0);
                MethodRequest methodRequest = new MethodRequest("PublishNodes", File.ReadAllBytes(fqPayloadFilename));
                await Hub.HandlePublishNodesMethodAsync(methodRequest, null).ConfigureAwait(false);
                await Task.Yield();
                Assert.True(NodeConfiguration.OpcSessions.Count == 1);
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions.Count == 1);
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 2000);
                await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
                _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
                Assert.True(NodeConfiguration.OpcSessions.Count == 1, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == 1, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == 1, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == 1, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == 2000);
            }
            finally
            {
                NodeConfiguration.Dispose();
                NodeConfiguration = null;
                Hub.Dispose();
                Hub = null;
            }
        }

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

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadPublishingInterval2000 =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_publishing_interval_2000.json"),
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadNoPublishingInterval =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_no_publishing_interval.json"),
                },
            };

        private readonly ITestOutputHelper _output;
        private static List<PublisherConfigurationFileEntryLegacyModel> _configurationFileEntries;
    }
}
