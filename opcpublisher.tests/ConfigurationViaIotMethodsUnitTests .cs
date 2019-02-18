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

    [Collection("Need PLC and publisher config")]
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
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "OpcPublishingInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadPublishingIntervalUnset))]
        public async void OpcPublishingIntervalUnsetAndIsNotPersisted(string testFilename, string payloadFilename)
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
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == OpcApplicationConfiguration.OpcPublishingInterval);
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
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "OpcPublishingInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadPublishingInterval2000))]
        public async void OpcPublishingInterval2000AndDifferentAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
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
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcPublishingInterval = 3000;

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
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "OpcPublishingInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadPublishingInterval2000))]
        public async void OpcPublishingInterval2000AndSameAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
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
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcPublishingInterval = 2000;

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

        /// <summary>
        /// Test that OpcSamplingInterval setting is not persisted when not configured via method.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "OpcSamplingInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadSamplingIntervalUnset))]
        public async void OpcSamplingIntervalUnsetAndIsNotPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcsamplinginterval/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcsamplinginterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].RequestedSamplingInterval == OpcApplicationConfiguration.OpcSamplingInterval);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcSamplingInterval == null);
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
        /// Test that OpcSamplingInterval setting is persisted when configured via method and is different as default.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "OpcSamplingInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadSamplingInterval2000))]
        public async void OpcSamplingInterval2000AndDifferentAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcsamplinginterval/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcsamplinginterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcSamplingInterval = 3000;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].RequestedSamplingInterval == 2000);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcSamplingInterval == 2000);
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
        /// Test that OpcSamplingInterval setting is persisted when configured via method and is same as default.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "OpcSamplingInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadSamplingInterval2000))]
        public async void OpcSamplingInterval2000AndSameAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcsamplinginterval/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/opcsamplinginterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcSamplingInterval = 2000;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].RequestedSamplingInterval == 2000);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcSamplingInterval == 2000);
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
        /// Test that SkipFirst setting is not persisted when default is false and not configured via method.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "SkipFirst")]
        [MemberData(nameof(PnPlcEmptyAndPayloadSkipFirstUnset))]
        public async void SkipFirsUnsetDefaultFalseAndIsNotPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.SkipFirstDefault = false;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].SkipFirst == false);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].SkipFirst == null);
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
        /// Test that SkipFirst setting is not persisted when default is true and not configured via method.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "SkipFirst")]
        [MemberData(nameof(PnPlcEmptyAndPayloadSkipFirstUnset))]
        public async void SkipFirstUnsetDefaultTrueAndIsNotPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.SkipFirstDefault = true;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].SkipFirst == true);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].SkipFirst == null);
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
        /// Test that skipFirst setting is persisted when configured true via method and is different as default.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "SkipFirst")]
        [MemberData(nameof(PnPlcEmptyAndPayloadSkipFirstTrue))]
        public async void SkipFirstTrueAndDifferentAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.SkipFirstDefault = false;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].SkipFirst == true);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].SkipFirst == true);
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
        /// Test that skipFirst setting is persisted when configured true via method and is save as default.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "SkipFirst")]
        [MemberData(nameof(PnPlcEmptyAndPayloadSkipFirstTrue))]
        public async void SkipFirstTrueAndSameAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.SkipFirstDefault = true;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].SkipFirst == true);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].SkipFirst == true);
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
        /// Test that skipFirst setting is persisted when configured false via method and is different as default.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "SkipFirst")]
        [MemberData(nameof(PnPlcEmptyAndPayloadSkipFirstFalse))]
        public async void SkipFirstFalseAndDifferentAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.SkipFirstDefault = true;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].SkipFirst == false);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].SkipFirst == false);
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
        /// Test that skipFirst setting is persisted when configured false via method and is same as default.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "SkipFirst")]
        [MemberData(nameof(PnPlcEmptyAndPayloadSkipFirstFalse))]
        public async void SkipFirstFalseAndSameAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/skipfirst/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.SkipFirstDefault = false;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].SkipFirst == false);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].SkipFirst == false);
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
        /// Test that HeartbeatInterval setting is not persisted when default is 0 not configured via method.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "HeartbeatInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadHeartbeatIntervalUnset))]
        public async void HeartbeatIntervalUnsetDefaultZeroAndIsNotPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/heartbeatinterval/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/heartbeatinterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.HeartbeatIntervalDefault = 0;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].HeartbeatInterval == 0);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].HeartbeatInterval == null);
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
        /// Test that HeartbeatInterval setting is not persisted when default is 2 and not configured via method.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "HeartbeatInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadHeartbeatIntervalUnset))]
        public async void HeartbeatIntervalUnsetDefaultNotZeroAndIsNotPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/heartbeatinterval/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/heartbeatinterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.HeartbeatIntervalDefault = 2;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].HeartbeatInterval == 2);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].HeartbeatInterval == null);
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
        /// Test that HeartbeatInterval setting is persisted when configured 2 via method and is different as default.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "HeartbeatInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadHeartbeatInterval2))]
        public async void HeartbeatInterval2AndDifferentAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/heartbeatinterval/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/heartbeatinterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.HeartbeatIntervalDefault = 1;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].HeartbeatInterval == 2);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].HeartbeatInterval == 2);
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
        /// Test that HeartbeatInterval setting is persisted when configured 2 via method and is save as default.
        /// </summary>
        [Theory]
        [Trait("Configuration", "DirectMethod")]
        [Trait("ConfigurationSetting", "HeartbeatInterval")]
        [MemberData(nameof(PnPlcEmptyAndPayloadHeartbeatInterval2))]
        public async void HeartbeatInterval2AndSameAsDefaultAndIsPersisted(string testFilename, string payloadFilename)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqPayloadFilename = $"{Directory.GetCurrentDirectory()}/testdata/heartbeatinterval/{payloadFilename}";
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/heartbeatinterval/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            UnitTestHelper.SetPublisherDefaults();
            OpcMonitoredItem.HeartbeatIntervalDefault = 2;

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
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].OpcMonitoredItems[0].HeartbeatInterval == 2);
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
                Assert.True(_configurationFileEntries[0].OpcNodes[0].HeartbeatInterval == 2);
            }
            finally
            {
                NodeConfiguration.Dispose();
                NodeConfiguration = null;
                Hub.Dispose();
                Hub = null;
            }
        }

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadPublishingInterval2000 =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_publishinginterval_2000.json"),
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadPublishingIntervalUnset =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_publishinginterval_unset.json"),
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadSamplingInterval2000 =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_samplinginterval_2000.json"),
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadSamplingIntervalUnset =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_samplinginterval_unset.json"),
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadSkipFirstUnset =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_skipfirst_unset.json"),
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadSkipFirstTrue =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_skipfirst_true.json"),
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadSkipFirstFalse =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_skipfirst_false.json"),
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadHeartbeatIntervalUnset =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_heartbeatinterval_unset.json"),
                },
            };

        public static IEnumerable<object[]> PnPlcEmptyAndPayloadHeartbeatInterval2 =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_empty.json"),
                    // method payload file
                    new string($"pn_plc_request_payload_heartbeatinterval_2.json"),
                },
            };

        private readonly ITestOutputHelper _output;
        private static List<PublisherConfigurationFileEntryLegacyModel> _configurationFileEntries;
    }
}
