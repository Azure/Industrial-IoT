using System.Collections.Generic;
using Xunit;

namespace OpcPublisher
{
    using Moq;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using Xunit.Abstractions;
    using static OpcApplicationConfiguration;
    using static Program;

    [Collection("Need PLC and app config")]
    public sealed class ConfigurationViaFileUnitTests : IDisposable
    {
        public ConfigurationViaFileUnitTests(ITestOutputHelper output)
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
        /// Test reading different configuration files and creating the correct internal data structures.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcSimple))]
        public void CreateOpcPublishingData(string testFilename, int configuredSessions,
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
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            try
            {
                NodeConfiguration = PublisherNodeConfiguration.Instance;
                Assert.True(NodeConfiguration.OpcSessions.Count == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == configuredSubscriptions, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == configuredMonitoredItems, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
            }
            finally
            {
                NodeConfiguration.Dispose();
                NodeConfiguration = null;
            }
        }

        /// <summary>
        /// Test that when no OpcPublishingInterval setting configured, it is not persisted.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcOpcPublishingIntervalNone))]
        public async void OpcPublishingIntervalSettingNotConfiguredAndNotPersisted(string testFilename, int configuredSessions,
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
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 3000;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
            var nodeConfigurationMockBase = new Mock<PublisherNodeConfiguration>();
            var nodeConfigurationMock = nodeConfigurationMockBase.As<IPublisherNodeConfiguration>();
            nodeConfigurationMock.CallBase = true;

            try
            {
                NodeConfiguration = PublisherNodeConfiguration.Instance;
                Assert.True(NodeConfiguration.OpcSessions.Count == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == configuredSubscriptions, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == configuredMonitoredItems, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 3000);
                await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
                _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
                Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == null);
            }
            finally
            {
                NodeConfiguration.Dispose();
                NodeConfiguration = null;
            }
        }

        /// <summary>
        /// Test that OpcPublishingInterval setting is kept when different as default setting.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcOpcPublishingInterval2000))]
        public async void OpcPublishingIntervalSettingConfiguredDifferentThanDefaultAndKeepsPersisted(string testFilename, int configuredSessions,
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
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 0;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            try
            {
                NodeConfiguration = PublisherNodeConfiguration.Instance;
                Assert.True(NodeConfiguration.OpcSessions.Count == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == configuredSubscriptions, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == configuredMonitoredItems, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 2000);
                await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
                _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
                Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == 2000);
            }
            finally
            {
                NodeConfiguration.Dispose();
                NodeConfiguration = null;
            }
        }

        /// <summary>
        /// Test that OpcPublishingInterval setting is not removed when default setting is the same.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcOpcPublishingInterval2000))]
        public async void OpcPublishingIntervalSettingConfiguredSameAsDefaultAndIsPersisted(string testFilename, int configuredSessions,
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
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            OpcPublishingInterval = 2000;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            try
            {
                NodeConfiguration = PublisherNodeConfiguration.Instance;
                Assert.True(NodeConfiguration.OpcSessions.Count == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == configuredSubscriptions, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == configuredMonitoredItems, "wrong # of monitored items");
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                Assert.True(NodeConfiguration.OpcSessions[0].OpcSubscriptions[0].RequestedPublishingInterval == 2000);
                await NodeConfiguration.UpdateNodeConfigurationFileAsync().ConfigureAwait(false);
                _configurationFileEntries = new List<PublisherConfigurationFileEntryLegacyModel>();
                _configurationFileEntries = JsonConvert.DeserializeObject<List<PublisherConfigurationFileEntryLegacyModel>>(File.ReadAllText(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));
                Assert.True(_configurationFileEntries[0].OpcNodes[0].OpcPublishingInterval == 2000);
            }
            finally
            {
                NodeConfiguration.Dispose();
                NodeConfiguration = null;
            }
            NodeConfiguration = null;
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

        private readonly ITestOutputHelper _output;
        private static List<PublisherConfigurationFileEntryLegacyModel> _configurationFileEntries;
    }
}
