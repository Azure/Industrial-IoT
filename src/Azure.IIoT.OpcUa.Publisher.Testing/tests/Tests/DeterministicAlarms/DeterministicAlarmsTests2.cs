// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using DeterministicAlarms;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class DeterministicAlarmsTests2<T>
    {
        public DeterministicAlarmsTests2(Func<INodeServices<T>> services, T connection,
            DeterministicAlarmsServer2 server)
        {
            _services = services;
            _connection = connection;
            _server = server;
        }

        public async Task BrowseAreaPathVendingMachine1DoorOpenTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Namespaces.DeterministicAlarmsInstance + "#s=VendingMachine1",
                BrowsePaths = new[] {
                    new[]
                    {
                        Namespaces.DeterministicAlarmsInstance + "#VendingMachine1_DoorOpen"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Namespaces.DeterministicAlarmsInstance + "#i=1", target.Target.NodeId);
        }

#if UNUSED
        public async Task VerifyThatTimeForEventsChangesEdgeFilteredAsync(CancellationToken ct = default)
        {
            // Subscribe to server events
            using var provider = _subscription();
            var services = _services();
            var connection = await _connection().ConfigureAwait(false);

            var monitoredItem = await provider.AddMonitoredItemAsync(new MonitoredItemModel
            {
                NodeId = Opc.Ua.ObjectIds.Server.ToString(),
                Attribute = NodeAttribute.EventNotifier,
                Settings = new MonitoredItemSettingsModel
                {
                    QueueSize = 1000
                }
            }).ConfigureAwait(false);

            const string doorOpen1 = Namespaces.DeterministicAlarmsInstance + "#i=1";

            await services.NodeShouldHaveStatesAsync(connection, doorOpen1,
                "Inactive", "Disabled").ConfigureAwait(false);

            var waitUntilStartInSeconds = TimeSpan.FromSeconds(5); // value in config
            _server.FireTimersWithPeriod(waitUntilStartInSeconds, 1);
            var opcEvent1 = await provider.GetEventChangesAsDictionary(1, true, Filter)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            var timeForFirstEvent = (DateTime)opcEvent1["/Time"];

            await services.NodeShouldHaveStatesAsync(connection, doorOpen1,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(waitUntilStartInSeconds, 1);
            var opcEvent2 = await provider.GetEventChangesAsDictionary(1, true, Filter)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            var timeForNextEvent = (DateTime)opcEvent2["/Time"];

            await services.NodeShouldHaveStatesAsync(connection, doorOpen1,
                "Inactive", "Disabled").ConfigureAwait(false);

            Assert.NotEqual(timeForFirstEvent, timeForNextEvent);

            static bool Filter(Dictionary<string, VariantValue> evt)
            {
                return ((string?)evt["/SourceNode"])?.StartsWith(
                    Namespaces.DeterministicAlarmsInstance, StringComparison.OrdinalIgnoreCase) == true;
            }
        }

        public async Task VerifyThatTimeForEventsChangesServerFilteredAsync(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(connection, new QueryCompilationRequestModel
            {
                Query = $@"
                    PREFIX ns <{Namespaces.DeterministicAlarmsInstance}>
                    SELECT * FROM BaseEventType
                    WHERE
                        /SourceNode IN (
                            'ns:s=VendingMachine1'^^NodeId,
                            'ns:s=VendingMachine2'^^NodeId
                        )
                "
            }).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);

            // Subscribe to server events
            using var provider = _subscription();

            var monitoredItem = await provider.AddMonitoredItemAsync(new MonitoredItemModel
            {
                NodeId = Opc.Ua.ObjectIds.Server.ToString(),
                Attribute = NodeAttribute.EventNotifier,
                Settings = new MonitoredItemSettingsModel
                {
                    EventFilter = result.EventFilter
                }
            }).ConfigureAwait(false);

            const string doorOpen1 = Namespaces.DeterministicAlarmsInstance + "#i=1";

            await services.NodeShouldHaveStatesAsync(connection, doorOpen1,
                "Inactive", "Disabled").ConfigureAwait(false);

            var waitUntilStartInSeconds = TimeSpan.FromSeconds(5); // value in config
            _server.FireTimersWithPeriod(waitUntilStartInSeconds, 1);
            var opcEvent1 = await provider.GetEventChangesAsDictionary(1)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            var timeForFirstEvent = (DateTime)opcEvent1["/Time"];

            await services.NodeShouldHaveStatesAsync(connection, doorOpen1,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(waitUntilStartInSeconds, 1);
            var opcEvent2 = await provider.GetEventChangesAsDictionary(1)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            var timeForNextEvent = (DateTime)opcEvent2["/Time"];

            await services.NodeShouldHaveStatesAsync(connection, doorOpen1,
                "Inactive", "Disabled").ConfigureAwait(false);

            Assert.NotEqual(timeForFirstEvent, timeForNextEvent);
        }
#endif
        private readonly Func<INodeServices<T>> _services;
        private readonly DeterministicAlarmsServer2 _server;
        private readonly T _connection;
    }
}
