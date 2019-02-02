using System.Collections.Generic;
using Xunit;

namespace OpcPublisher
{
    using Microsoft.Azure.Devices.Client;
    using Moq;
    using System;
    using System.IO;
    using System.Threading;
    using Xunit.Abstractions;
    using static Program;

    [Collection("Need PLC and app config")]
    public sealed class TelemetryUnitTests : IDisposable
    {
        public TelemetryUnitTests(ITestOutputHelper output)
        {
            // xunit output
            _output = output;

            // init static publisher objects
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
        /// Test telemetry is sent to the hub.
        /// </summary>
        [Theory]
        [MemberData(nameof(PnPlcCurrentTime))]
        public void TelemetryIsSent(string testFilename, int configuredSessions,
            int configuredSubscriptions, int configuredMonitoredItems)
        {
            string methodName = UnitTestHelper.GetMethodName();
            string fqTempFilename = string.Empty;
            string fqTestFilename = $"{Directory.GetCurrentDirectory()}/testdata/telemetry/{testFilename}";
            fqTempFilename = $"{Directory.GetCurrentDirectory()}/tempdata/{methodName}_{testFilename}";
            if (File.Exists(fqTempFilename))
            {
                File.Delete(fqTempFilename);
            }
            File.Copy(fqTestFilename, fqTempFilename);
            PublisherNodeConfiguration.PublisherNodeConfigurationFilename = fqTempFilename;
            _output.WriteLine($"now testing: {PublisherNodeConfiguration.PublisherNodeConfigurationFilename}");
            Assert.True(File.Exists(PublisherNodeConfiguration.PublisherNodeConfigurationFilename));

            // mock IoTHub communication
            var hubMockBase = new Mock<HubCommunicationBase>();
            var hubMock = hubMockBase.As<IHubCommunication>();
            hubMock.CallBase = true;
            Hub = hubMock.Object;

            // configure hub client mock
            var hubClientMockBase = new Mock<HubClient>();
            var hubClientMock = hubClientMockBase.As<IHubClient>();
            int eventsReceived = 0;
            hubClientMock.Setup(m => m.SendEventAsync(It.IsAny<Message>())).Callback<Message>(m => eventsReceived++);
            IotHubCommunication.IotHubClient = hubClientMock.Object;
            Hub.InitHubCommunicationAsync(hubClientMockBase.Object).Wait();

            try
            {
                const int eventToTest = 2;
                NodeConfiguration = PublisherNodeConfiguration.Instance;
                Assert.True(NodeConfiguration.OpcSessions.Count == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSessionsConfigured == configuredSessions, "wrong # of sessions");
                Assert.True(NodeConfiguration.NumberOfOpcSubscriptionsConfigured == configuredSubscriptions, "wrong # of subscriptions");
                Assert.True(NodeConfiguration.NumberOfOpcMonitoredItemsConfigured == configuredMonitoredItems, "wrong # of monitored items");
                int seconds = UnitTestHelper.WaitTilItemsAreMonitored();
                Thread.Sleep(eventToTest * 1000);
                _output.WriteLine($"sessions configured {NodeConfiguration.NumberOfOpcSessionsConfigured}, connected {NodeConfiguration.NumberOfOpcSessionsConnected}");
                _output.WriteLine($"subscriptions configured {NodeConfiguration.NumberOfOpcSubscriptionsConfigured}, connected {NodeConfiguration.NumberOfOpcSubscriptionsConnected}");
                _output.WriteLine($"items configured {NodeConfiguration.NumberOfOpcMonitoredItemsConfigured}, monitored {NodeConfiguration.NumberOfOpcMonitoredItemsMonitored}, toRemove {NodeConfiguration.NumberOfOpcMonitoredItemsToRemove}");
                hubClientMock.VerifySet(m => m.ProductInfo = "OpcPublisher");
                hubClientMock.Verify(hc => hc.SendEventAsync(It.IsAny<Message>()), Times.AtLeastOnce(), "SendEventAsync was never called");
                _output.WriteLine($"waited {seconds} seconds till monitoring started, events generated {eventsReceived}");
                Assert.True(eventsReceived >= eventToTest);
            }
            finally
            {
                NodeConfiguration.Dispose();
                NodeConfiguration = null;
                Hub.Dispose();
                IotHubCommunication.IotHubClient = null;
                Hub = null;
            }
        }

        public static IEnumerable<object[]> PnPlcCurrentTime =>
            new List<object[]>
            {
                new object[] {
                    // published nodes configuration file
                    new string($"pn_plc_currenttime.json"),
                    // # of configured sessions
                    1,
                    // # of configured subscriptions
                    1,
                    // # of configured monitored items
                    1
                },
            };

        private readonly ITestOutputHelper _output;
    }
}
