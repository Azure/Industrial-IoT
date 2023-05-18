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

    public class DeterministicAlarmsTests1<T>
    {
        public DeterministicAlarmsTests1(Func<INodeServices<T>> services, T connection,
            DeterministicAlarmsServer1 server)
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

        public async Task BrowseAreaPathVendingMachine2DoorOpenTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Namespaces.DeterministicAlarmsInstance + "#s=VendingMachine2",
                BrowsePaths = new[] {
                    new[]
                    {
                        Namespaces.DeterministicAlarmsInstance + "#VendingMachine2_DoorOpen"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Namespaces.DeterministicAlarmsInstance + "#i=236", target.Target.NodeId);
        }

        public async Task BrowseAreaPathVendingMachine1TemperatureHighTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Namespaces.DeterministicAlarmsInstance + "#s=VendingMachine1",
                BrowsePaths = new[] {
                    new[]
                    {
                        Namespaces.DeterministicAlarmsInstance + "#VendingMachine1_TemperatureHigh"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Namespaces.DeterministicAlarmsInstance + "#i=115", target.Target.NodeId);
        }

        public async Task BrowseAreaPathVendingMachine2LightOffTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Namespaces.DeterministicAlarmsInstance + "#s=VendingMachine2",
                BrowsePaths = new[] {
                    new[]
                    {
                        Namespaces.DeterministicAlarmsInstance + "#VendingMachine2_LightOff"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Namespaces.DeterministicAlarmsInstance + "#i=350", target.Target.NodeId);
        }

#if UNUSED
        public async Task FiresEventSequenceTestWithEdgeFilteringAsync(CancellationToken ct = default)
        {
            // Subscribe to server events
            using var provider = _subscription();
            var services = _services();


            var monitoredItem = await provider.AddMonitoredItemAsync(new MonitoredItemModel
            {
                NodeId = Opc.Ua.ObjectIds.Server.ToString(),
                Attribute = NodeAttribute.EventNotifier,
                Settings = new MonitoredItemSettingsModel
                {
                    QueueSize = 1000
                }
            }).ConfigureAwait(false);

            const string machine1 = Namespaces.DeterministicAlarmsInstance + "#s=VendingMachine1";
            const string machine2 = Namespaces.DeterministicAlarmsInstance + "#s=VendingMachine2";
            const string doorOpen1 = Namespaces.DeterministicAlarmsInstance + "#i=1";
            const string tempHigh1 = Namespaces.DeterministicAlarmsInstance + "#i=110";
            const string doorOpen2 = Namespaces.DeterministicAlarmsInstance + "#i=226";
            const string lightOff2 = Namespaces.DeterministicAlarmsInstance + "#i=335";

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, tempHigh1,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, doorOpen2,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Inactive", "Disabled").ConfigureAwait(false);

            var waitUntilStartInSeconds = TimeSpan.FromSeconds(9); // value in *.json file
            _server.FireTimersWithPeriod(waitUntilStartInSeconds, 1);
            var opcEvent1 = await provider.GetEventChangesAsDictionary(1, true, Filter)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent1["/EventId"], Encoding.UTF8.GetBytes("V1_DoorOpen-1 (1)"));
            Assert.Equal(opcEvent1["/EventType"], Opc.Ua.ObjectTypeIds.TripAlarmType.ToString());
            Assert.Equal(opcEvent1["/SourceNode"], machine1);
            Assert.Equal(opcEvent1["/SourceName"], "VendingMachine1");
            Assert.Equal(opcEvent1["/Message"].GetByPath("Text"), "Door Open");
            Assert.Equal(opcEvent1["/Severity"], (int)Opc.Ua.EventSeverity.High);

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Active", "Enabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, tempHigh1,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, doorOpen2,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Inactive", "Disabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(5), 1);
            var opcEvent2 = await provider.GetEventChangesAsDictionary(1, true, Filter)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent2["/EventId"], Encoding.UTF8.GetBytes("V2_LightOff-1 (1)"));
            Assert.Equal(opcEvent2["/EventType"], Opc.Ua.ObjectTypeIds.OffNormalAlarmType.ToString());
            Assert.Equal(opcEvent2["/SourceNode"], machine2);
            Assert.Equal(opcEvent2["/SourceName"], "VendingMachine2");
            Assert.Equal(opcEvent2["/Message"].GetByPath("Text"), "Light Off in machine");
            Assert.Equal(opcEvent2["/Severity"], (int)Opc.Ua.EventSeverity.Medium);

            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(7), 1);
            var opcEvent3 = await provider.GetEventChangesAsDictionary(1, true, Filter)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent3["/EventId"], Encoding.UTF8.GetBytes("V1_DoorOpen-2 (1)"));
            Assert.Equal(opcEvent3["/EventType"], Opc.Ua.ObjectTypeIds.TripAlarmType.ToString());
            Assert.Equal(opcEvent3["/SourceNode"], machine1);
            Assert.Equal(opcEvent3["/SourceName"], "VendingMachine1");
            Assert.Equal(opcEvent3["/Message"].GetByPath("Text"), "Door Closed");
            Assert.Equal(opcEvent3["/Severity"], (int)Opc.Ua.EventSeverity.Medium);

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Inactive", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(4), 1);
            var opcEvent4 = await provider.GetEventChangesAsDictionary(1, true, Filter)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent4["/EventId"], Encoding.UTF8.GetBytes("V1_TemperatureHigh-1 (1)"));
            Assert.Equal(opcEvent4["/EventType"], Opc.Ua.ObjectTypeIds.LimitAlarmType.ToString());
            Assert.Equal(opcEvent4["/SourceNode"], machine1);
            Assert.Equal(opcEvent4["/SourceName"], "VendingMachine1");
            Assert.Equal(opcEvent4["/Message"].GetByPath("Text"), "Temperature is HIGH");
            Assert.Equal(opcEvent4["/Severity"], (int)Opc.Ua.EventSeverity.High);

            await services.NodeShouldHaveStatesAsync(_connection, tempHigh1,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1);
            var opcEvent5 = await provider.GetEventChangesAsDictionary(1, true, Filter)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent5["/EventId"], Encoding.UTF8.GetBytes("V1_DoorOpen-1 (2)"));
            Assert.Equal(opcEvent5["/Message"].GetByPath("Text"), "Door Open");

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(5), 1);
            var opcEvent6 = await provider.GetEventChangesAsDictionary(1, true, Filter)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent6["/EventId"], Encoding.UTF8.GetBytes("V2_LightOff-1 (2)"));
            Assert.Equal(opcEvent6["/Message"].GetByPath("Text"), "Light Off in machine");

            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            // At this point, the *runningForSeconds* limit in the JSON config causes execution to stop
            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1);

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Active", "Enabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, tempHigh1,
                "Active", "Enabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, doorOpen2,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Active", "Enabled").ConfigureAwait(false);

            static bool Filter(Dictionary<string, VariantValue> evt)
            {
                return ((string?)evt["/SourceNode"])?.StartsWith(
                    Namespaces.DeterministicAlarmsInstance, StringComparison.OrdinalIgnoreCase) == true;
            }
        }

        public async Task FiresEventSequenceTestWithServerFilteringAsync(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
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

            const string machine1 = Namespaces.DeterministicAlarmsInstance + "#s=VendingMachine1";
            const string machine2 = Namespaces.DeterministicAlarmsInstance + "#s=VendingMachine2";
            const string doorOpen1 = Namespaces.DeterministicAlarmsInstance + "#i=1";
            const string tempHigh1 = Namespaces.DeterministicAlarmsInstance + "#i=110";
            const string doorOpen2 = Namespaces.DeterministicAlarmsInstance + "#i=226";
            const string lightOff2 = Namespaces.DeterministicAlarmsInstance + "#i=335";

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, tempHigh1,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, doorOpen2,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Inactive", "Disabled").ConfigureAwait(false);

            var waitUntilStartInSeconds = TimeSpan.FromSeconds(9); // value in *.json file
            _server.FireTimersWithPeriod(waitUntilStartInSeconds, 1);
            var opcEvent1 = await provider.GetEventChangesAsDictionary(1)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent1["/EventId"], Encoding.UTF8.GetBytes("V1_DoorOpen-1 (1)"));
            Assert.Equal(opcEvent1["/EventType"], Opc.Ua.ObjectTypeIds.TripAlarmType.ToString());
            Assert.Equal(opcEvent1["/SourceNode"], machine1);
            Assert.Equal(opcEvent1["/SourceName"], "VendingMachine1");
            Assert.Equal(opcEvent1["/Message"].GetByPath("Text"), "Door Open");
            Assert.Equal(opcEvent1["/Severity"], (int)Opc.Ua.EventSeverity.High);

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Active", "Enabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, tempHigh1,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, doorOpen2,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Inactive", "Disabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(5), 1);
            var opcEvent2 = await provider.GetEventChangesAsDictionary(1)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent2["/EventId"], Encoding.UTF8.GetBytes("V2_LightOff-1 (1)"));
            Assert.Equal(opcEvent2["/EventType"], Opc.Ua.ObjectTypeIds.OffNormalAlarmType.ToString());
            Assert.Equal(opcEvent2["/SourceNode"], machine2);
            Assert.Equal(opcEvent2["/SourceName"], "VendingMachine2");
            Assert.Equal(opcEvent2["/Message"].GetByPath("Text"), "Light Off in machine");
            Assert.Equal(opcEvent2["/Severity"], (int)Opc.Ua.EventSeverity.Medium);

            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(7), 1);
            var opcEvent3 = await provider.GetEventChangesAsDictionary(1)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent3["/EventId"], Encoding.UTF8.GetBytes("V1_DoorOpen-2 (1)"));
            Assert.Equal(opcEvent3["/EventType"], Opc.Ua.ObjectTypeIds.TripAlarmType.ToString());
            Assert.Equal(opcEvent3["/SourceNode"], machine1);
            Assert.Equal(opcEvent3["/SourceName"], "VendingMachine1");
            Assert.Equal(opcEvent3["/Message"].GetByPath("Text"), "Door Closed");
            Assert.Equal(opcEvent3["/Severity"], (int)Opc.Ua.EventSeverity.Medium);

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Inactive", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(4), 1);
            var opcEvent4 = await provider.GetEventChangesAsDictionary(1)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent4["/EventId"], Encoding.UTF8.GetBytes("V1_TemperatureHigh-1 (1)"));
            Assert.Equal(opcEvent4["/EventType"], Opc.Ua.ObjectTypeIds.LimitAlarmType.ToString());
            Assert.Equal(opcEvent4["/SourceNode"], machine1);
            Assert.Equal(opcEvent4["/SourceName"], "VendingMachine1");
            Assert.Equal(opcEvent4["/Message"].GetByPath("Text"), "Temperature is HIGH");
            Assert.Equal(opcEvent4["/Severity"], (int)Opc.Ua.EventSeverity.High);

            await services.NodeShouldHaveStatesAsync(_connection, tempHigh1,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1);
            var opcEvent5 = await provider.GetEventChangesAsDictionary(1)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent5["/EventId"], Encoding.UTF8.GetBytes("V1_DoorOpen-1 (2)"));
            Assert.Equal(opcEvent5["/Message"].GetByPath("Text"), "Door Open");

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(5), 1);
            var opcEvent6 = await provider.GetEventChangesAsDictionary(1)
                .FirstAsync(CancellationToken ct = default).ConfigureAwait(false);
            Assert.Equal(opcEvent6["/EventId"], Encoding.UTF8.GetBytes("V2_LightOff-1 (2)"));
            Assert.Equal(opcEvent6["/Message"].GetByPath("Text"), "Light Off in machine");

            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Active", "Enabled").ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1), 1); // advance to next step

            // At this point, the *runningForSeconds* limit in the JSON config causes execution to stop
            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1);

            await services.NodeShouldHaveStatesAsync(_connection, doorOpen1,
                "Active", "Enabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, tempHigh1,
                "Active", "Enabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, doorOpen2,
                "Inactive", "Disabled").ConfigureAwait(false);
            await services.NodeShouldHaveStatesAsync(_connection, lightOff2,
                "Active", "Enabled").ConfigureAwait(false);
        }
#endif

        private readonly Func<INodeServices<T>> _services;
        private readonly DeterministicAlarmsServer1 _server;
        private readonly T _connection;
    }
}
