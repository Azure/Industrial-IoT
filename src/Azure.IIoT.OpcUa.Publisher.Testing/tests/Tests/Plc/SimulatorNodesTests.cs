// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using FluentAssertions;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for the variables defined in the simulator, such
    /// as fast-changing and trended nodes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimulatorNodesTests<T>
    {
        public SimulatorNodesTests(BaseServerFixture server,
            Func<INodeServices<T>> services, T connection)
        {
            _server = server;
            _services = services;
            _connection = connection;
        }

        public async Task TelemetryStepUpTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            // need to track the first value encountered b/c the measurement stream starts when
            // the server starts and it can take several seconds for our test to start
            uint? firstValue = null;
            var measurements = new List<object?>();
            for (var i = 0; i < 10; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 1);

                var value = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=StepUp"
                }, ct).ConfigureAwait(false);
                firstValue ??= (uint?)value?.Value;
                measurements.Add((uint?)value?.Value);
            }

            var expectedValues = Enumerable.Range((int)(firstValue ?? 0u), 10)
                .Select<int, object>(i => (uint)i)
                .ToList();

            measurements.Should().NotBeEmpty()
                .And.HaveCount(10)
                .And.ContainInOrder(expectedValues)
                .And.ContainItemsAssignableTo<uint>();
        }

        public async Task TelemetryFastNodeTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            uint? lastValue = null;
            for (var i = 0; i < 10; i++)
            {
                var value = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
                }, ct).ConfigureAwait(false);
                if (lastValue == null)
                {
                    lastValue = (uint?)value?.Value;
                }
                else
                {
                    Assert.Equal(lastValue + 1, (uint)(value?.Value ?? 0));
                    lastValue = (uint?)value?.Value;
                }

                _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1);
            }

            lastValue++;

            await services.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                ObjectId = Plc.Namespaces.PlcApplications + "#s=Methods",
                MethodId = Plc.Namespaces.PlcApplications + "#s=StopUpdateFastNodes"
            }, ct).ConfigureAwait(false);

            var nextValue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
            }, ct).ConfigureAwait(false);
            Assert.Equal(lastValue, (uint?)nextValue?.Value);

            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1);
            nextValue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
            }, ct).ConfigureAwait(false);
            Assert.Equal(lastValue, (uint?)nextValue?.Value);
            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1);

            await services.MethodCallAsync(_connection, new MethodCallRequestModel
            {
                ObjectId = Plc.Namespaces.PlcApplications + "#s=Methods",
                MethodId = Plc.Namespaces.PlcApplications + "#s=StartUpdateFastNodes"
            }, ct).ConfigureAwait(false);
            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1);

            nextValue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
            }, ct).ConfigureAwait(false);
            Assert.Equal(lastValue + 1, (uint?)nextValue?.Value);
        }

        public async Task TelemetryContainsOutlierInDipDataAsync(CancellationToken ct = default)
        {
            var services = _services();

            const int outlierValue = -1000;
            var outlierCount = 0;
            var maxValue = 0d;
            var minValue = 0d;

            // take 300 measurements, which is enough that at least a few outliers should be present
            for (var i = 0; i < 300; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 1);

                var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=DipData"
                }, ct).ConfigureAwait(false);

                var value = (double?)rawvalue?.Value;
                if (Math.Round(value ?? 0.0) == outlierValue)
                {
                    outlierCount++;
                }
                else
                {
                    maxValue = Math.Max(maxValue, value ?? double.MaxValue);
                    minValue = Math.Min(minValue, value ?? 0.0);
                }
            }

            maxValue.Should().BeInRange(90, 100,
                "measurement data should have a ceiling around 100");
            minValue.Should().BeInRange(-100, -90,
                "measurement data should have a floor around -100");
            outlierCount.Should().BeGreaterThan(0,
                "there should be at least a few measurements that were {0}", outlierValue);
        }

        public async Task TelemetryContainsOutlierInSpikeDataAsync(CancellationToken ct = default)
        {
            var services = _services();

            const int outlierValue = 1000;
            var outlierCount = 0;
            var maxValue = 0d;
            var minValue = 0d;

            // take 300 measurements, which is enough that at least a few outliers should be present
            for (var i = 0; i < 300; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 1);

                var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=SpikeData"
                }, ct).ConfigureAwait(false);

                var value = (double?)rawvalue?.Value;
                if (Math.Round(value ?? 0.0) == outlierValue)
                {
                    outlierCount++;
                }
                else
                {
                    maxValue = Math.Max(maxValue, value ?? double.MaxValue);
                    minValue = Math.Min(minValue, value ?? 0.0);
                }
            }

            maxValue.Should().BeInRange(90, 100,
                "measurement data should have a ceiling around 100");
            minValue.Should().BeInRange(-100, -90,
                "measurement data should have a floor around -100");
            outlierCount.Should().BeGreaterThan(0,
                "there should be at least a few measurements that were {0}", outlierValue);
        }

        public async Task RandomSignedInt32TelemetryChangesWithPeriodAsync(CancellationToken ct = default)
        {
            var services = _services();

            int? lastValue = null;
            var numberOfValueChanges = 0;
            for (var i = 0; i < 4; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 1);

                var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=RandomSignedInt32"
                }, ct).ConfigureAwait(false);
                var value = (int?)rawvalue?.Value;
                if (i > 0 && value?.CompareTo(lastValue) != 0)
                {
                    numberOfValueChanges++;
                }
                lastValue = value;
            }
            numberOfValueChanges.Should().Be(3);
        }

        public async Task RandomUnsignedInt32TelemetryChangesWithPeriodAsync(CancellationToken ct = default)
        {
            var services = _services();

            uint? lastValue = null;
            var numberOfValueChanges = 0;
            for (var i = 0; i < 4; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 1);

                var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=RandomUnsignedInt32"
                }, ct).ConfigureAwait(false);
                var value = (uint?)rawvalue?.Value;
                if (i > 0 && value?.CompareTo(lastValue) != 0)
                {
                    numberOfValueChanges++;
                }
                lastValue = value;
            }
            numberOfValueChanges.Should().Be(3);
        }

        public async Task AlternatingBooleanTelemetryChangesWithPeriodAsync(CancellationToken ct = default)
        {
            var services = _services();

            bool? lastValue = null;
            var numberOfValueChanges = 0;
            for (var i = 0; i < 4; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 50);

                var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=AlternatingBoolean"
                }, ct).ConfigureAwait(false);
                var value = (bool?)rawvalue.Value;
                if (i > 0 && value?.CompareTo(lastValue) != 0)
                {
                    numberOfValueChanges++;
                }
                lastValue = value;
            }
            numberOfValueChanges.Should().Be(3);
        }

        public async Task NegativeTrendDataTelemetryChangesWithPeriodAsync(CancellationToken ct = default)
        {
            var services = _services();

            const uint periodInMilliseconds = 100u;
            const int invocations = 50;
            const int rampUpPeriods = kRampUpPeriods;
            const int numberOfTimes = invocations * rampUpPeriods;

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), numberOfTimes);

            // Measure the value 4 times, sleeping for a third of the period at which the value changes each time.
            // The number of times the value changes over the 4 measurements should be between 1 and 2.
            double? lastValue = null;
            var numberOfValueChanges = 0;
            for (var i = 0; i < 4; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), invocations);

                var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=NegativeTrendData"
                }, ct).ConfigureAwait(false);
                var value = (double?)rawvalue?.Value;
                if (i > 0 && value?.CompareTo(lastValue) != 0)
                {
                    numberOfValueChanges++;
                }
                lastValue = value;
            }
            numberOfValueChanges.Should().Be(3);
        }

        public async Task PositiveTrendDataTelemetryChangesWithPeriodAsync(CancellationToken ct = default)
        {
            var services = _services();

            const uint periodInMilliseconds = 100u;
            const int invocations = 50;
            const int rampUpPeriods = kRampUpPeriods;
            const int numberOfTimes = invocations * rampUpPeriods;

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), numberOfTimes);

            double? lastValue = null;
            var numberOfValueChanges = 0;
            for (var i = 0; i < 4; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), invocations);

                var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=PositiveTrendData"
                }, ct).ConfigureAwait(false);
                var value = (double?)rawvalue?.Value;
                if (i > 0 && value?.CompareTo(lastValue) != 0)
                {
                    numberOfValueChanges++;
                }
                lastValue = value;
            }
            numberOfValueChanges.Should().Be(3);
        }

        public async Task SlowUIntScalar1TelemetryChangesWithPeriodAsync(CancellationToken ct = default)
        {
            var services = _services();

            uint? lastValue = null;
            var numberOfValueChanges = 0;
            for (var i = 0; i < 4; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(10000), 1);

                var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=SlowUIntScalar1"
                }, ct).ConfigureAwait(false);
                var value = (uint?)rawvalue?.Value;
                if (i > 0 && value?.CompareTo(lastValue) != 0)
                {
                    numberOfValueChanges++;
                }
                lastValue = value;
            }
            numberOfValueChanges.Should().Be(3);
        }

        public async Task FastUIntScalar1TelemetryChangesWithPeriodAsync(CancellationToken ct = default)
        {
            var services = _services();

            uint? lastValue = null;
            var numberOfValueChanges = 0;
            for (var i = 0; i < 4; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(1000), 1);

                var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
                }, ct).ConfigureAwait(false);
                var value = (uint?)rawvalue?.Value;
                if (i > 0 && value?.CompareTo(lastValue) != 0)
                {
                    numberOfValueChanges++;
                }
                lastValue = value;
            }
            numberOfValueChanges.Should().Be(3);
        }

        public async Task BadNodeHasAlternatingStatusCodeAsync(CancellationToken ct = default)
        {
            var services = _services();

            const uint periodInMilliseconds = 1000u;
            const int invocations = 1;
            const int cycles = 15;
            var values = await AsyncEnumerable.Range(0, cycles)
                .SelectAwait(async i =>
                {
                    _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), invocations);
                    var value = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                    {
                        NodeId = Plc.Namespaces.PlcApplications + "#s=BadFastUIntScalar1"
                    }).ConfigureAwait(false);
                    return (value.ErrorInfo?.StatusCode ?? StatusCodes.Good, value.Value);
                }).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            var valuesByStatus = values.GroupBy(v => v.Item1).ToDictionary(g => g.Key, g => g.ToList());

            valuesByStatus
                .Keys.Should().BeEquivalentTo(new[]
                {
                    StatusCodes.Good,
                    StatusCodes.UncertainLastUsableValue,
                    StatusCodes.BadDataLost,
                    StatusCodes.BadNoCommunication
                });

            valuesByStatus
                .Should().ContainKey(StatusCodes.Good)
                .WhoseValue
                .Should().HaveCountGreaterThan(cycles * 5 / 10)
                .And.OnlyContain(v => v.Value != null);

            valuesByStatus
                .Should().ContainKey(StatusCodes.UncertainLastUsableValue)
                .WhoseValue
                .Should().OnlyContain(v => v.Value != null);
        }

        public async Task FastLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync(CancellationToken ct = default)
        {
            var services = _services();

            const uint periodInMilliseconds = 1000u;
            var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
            }, ct).ConfigureAwait(false);
            var value1 = (uint?)rawvalue?.Value;

            // Change the value of the NumberOfUpdates control variable to 6.
            await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=FastNumberOfUpdates",
                Value = 6
            }, ct).ConfigureAwait(false);

            // Fire the timer 6 times, should increase the value each time.
            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), 6);
            rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
            }, ct).ConfigureAwait(false);

            var value2 = (uint?)rawvalue?.Value;
            value2.Should().Be(value1 + 6);

            // NumberOfUpdates variable should now be 0. The Fast node value should not change anymore.
            for (var i = 0; i < 10; i++)
            {
                rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=FastNumberOfUpdates"
                }, ct).ConfigureAwait(false);
                ((int?)rawvalue?.Value).Should().Be(0);
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), 1);
                rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
                }, ct).ConfigureAwait(false);

                var value3 = (uint?)rawvalue?.Value;
                value3.Should().Be(value1 + 6);
            }

            // Change the value of the NumberOfUpdates control variable to -1.
            // The Fast node value should now increase indefinitely.
            await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=FastNumberOfUpdates",
                Value = kNoLimit
            }, ct).ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), 3);
            rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=FastUIntScalar1"
            }, ct).ConfigureAwait(false);

            var value4 = (uint?)rawvalue?.Value;
            value4.Should().Be(value1 + 6 + 3);
            rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=FastNumberOfUpdates"
            }, ct).ConfigureAwait(false);
            ((int?)rawvalue?.Value).Should().Be(kNoLimit,
                "NumberOfUpdates node value should not change when it is {0}", kNoLimit);
        }

        public async Task SlowLimitNumberOfUpdatesStopsUpdatingAfterLimitAsync(CancellationToken ct = default)
        {
            var services = _services();

            const uint periodInMilliseconds = 10000u;
            var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=SlowUIntScalar1"
            }, ct).ConfigureAwait(false);
            var value1 = (uint?)rawvalue?.Value;

            // Change the value of the NumberOfUpdates control variable to 6.
            await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=SlowNumberOfUpdates",
                Value = 6
            }, ct).ConfigureAwait(false);

            // Fire the timer 6 times, should increase the value each time.
            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), 6);
            rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=SlowUIntScalar1"
            }, ct).ConfigureAwait(false);

            var value2 = (uint?)rawvalue?.Value;
            value2.Should().Be(value1 + 6);

            // NumberOfUpdates variable should now be 0. The Fast node value should not change anymore.
            for (var i = 0; i < 10; i++)
            {
                rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=SlowNumberOfUpdates"
                }, ct).ConfigureAwait(false);
                ((int?)rawvalue?.Value).Should().Be(0);
                _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), 1);
                rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcApplications + "#s=SlowUIntScalar1"
                }, ct).ConfigureAwait(false);

                var value3 = (uint?)rawvalue?.Value;
                value3.Should().Be(value1 + 6);
            }

            // Change the value of the NumberOfUpdates control variable to -1.
            // The Fast node value should now increase indefinitely.
            await services.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=SlowNumberOfUpdates",
                Value = kNoLimit
            }, ct).ConfigureAwait(false);

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(periodInMilliseconds), 3);
            rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=SlowUIntScalar1"
            }, ct).ConfigureAwait(false);

            var value4 = (uint?)rawvalue?.Value;
            value4.Should().Be(value1 + 6 + 3);
            rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=SlowNumberOfUpdates"
            }, ct).ConfigureAwait(false);
            ((int?)rawvalue?.Value).Should().Be(kNoLimit,
                "NumberOfUpdates node value should not change when it is {0}", kNoLimit);
        }

        public async Task PositiveTrendDataNodeHasValueWithTrendAsync(CancellationToken ct = default)
        {
            var services = _services();

            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 50 * kRampUpPeriods);

            var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=PositiveTrendData"
            }, ct).ConfigureAwait(false);

            var firstValue = (double?)rawvalue?.Value;
            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 50);

            rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=PositiveTrendData"
            }, ct).ConfigureAwait(false);
            var secondValue = (double?)rawvalue?.Value;

            secondValue.Should().BeGreaterThan(firstValue ?? double.MaxValue);
        }

        public async Task NegativeTrendDataNodeHasValueWithTrendAsync(CancellationToken ct = default)
        {
            var services = _services();
            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 50 * kRampUpPeriods);

            var rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=NegativeTrendData"
            }, ct).ConfigureAwait(false);

            var firstValue = (double?)rawvalue?.Value;
            _server.FireTimersWithPeriod(TimeSpan.FromMilliseconds(100), 50);

            rawvalue = await services.ValueReadAsync(_connection, new ValueReadRequestModel
            {
                NodeId = Plc.Namespaces.PlcApplications + "#s=NegativeTrendData"
            }, ct).ConfigureAwait(false);
            var secondValue = (double?)rawvalue?.Value;
            secondValue.Should().BeLessThan(firstValue ?? 0.0);
        }

        /// <summary>
        /// Simulator does not update trended and boolean values in the first few cycles
        /// (a random number of cycles between 1 and 10)
        /// </summary>
        private const int kRampUpPeriods = 10;

        /// <summary>
        /// Value set for NumberOfUpdates for the simulator to update value indefinitely.
        /// </summary>
        private const int kNoLimit = -1;
        private readonly T _connection;
        private readonly BaseServerFixture _server;
        private readonly Func<INodeServices<T>> _services;
    }
}
