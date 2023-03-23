// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using FluentAssertions;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using PlcModel;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for the Plc model, which is a complex type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class PlcModelComplexTypeTests<T>
    {
        public PlcModelComplexTypeTests(BaseServerFixture server,
            Func<INodeServices<T>> services, T connection)
        {
            _server = server;
            _services = services;
            _connection = connection;
        }

        public async Task PlcModelHeaterTestsAsync()
        {
            var services = _services();

            await TurnHeaterOnAsync().ConfigureAwait(false);
            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1000);

            var model = await GetPlcModelAsync().ConfigureAwait(false);
            var state = model?.HeaterState;
            var temperature = model?.Temperature;
            var pressure = model?.Pressure;

            state.Should().Be(PlcHeaterStateType.On,
                "heater should start in 'on' state");
            pressure.Should().BeGreaterThan(10_000,
                "pressure should start at 10k and get higher");
            temperature?.Top.Should().Be(pressure - 100_005,
                "top is always 100,005 less than pressure. Pressure: {0}", pressure);
            temperature?.Bottom.Should().Be(pressure - 100_000,
                "bottom is always 100,000 less than pressure. Pressure: {0}", pressure);

            var previousPressure = 0;
            for (var i = 0; i < 5; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1000);
                model = await GetPlcModelAsync().ConfigureAwait(false);
                pressure = model?.Pressure;

                pressure.Should().BeGreaterThan(previousPressure,
                    "pressure should build when heater is on");
                previousPressure = pressure ?? int.MaxValue;
            }

            // let heater run for a few seconds to make temperature rise
            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1000);
            await TurnHeaterOffAsync().ConfigureAwait(false);
            _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1000);

            model = await GetPlcModelAsync().ConfigureAwait(false);
            state = model?.HeaterState;
            temperature = model?.Temperature;
            pressure = model?.Pressure;

            state.Should().Be(PlcHeaterStateType.Off,
                "heater should have been turned off");
            pressure.Should().BeGreaterThan(10_000,
                "pressure should start at 10k and get higher");

            temperature?.Top.Should().Be(pressure - 100_005,
                "top is always 100,005 less than pressure. Pressure: {0}", pressure);
            temperature?.Bottom.Should().Be(pressure - 100_000,
                "btoom is always 100,000 less than pressure. Pressure: {0}", pressure);

            await TurnHeaterOffAsync().ConfigureAwait(false);
            for (var i = 0; i < 5; i++)
            {
                _server.FireTimersWithPeriod(TimeSpan.FromSeconds(1), 1000);
                model = await GetPlcModelAsync().ConfigureAwait(false);
                pressure = model?.Pressure;

                pressure.Should().BeLessThan(previousPressure,
                    "pressure should drop when heater is off");
                previousPressure = pressure ?? 0;
            }

            async Task TurnHeaterOnAsync()
            {
                var result = await services.MethodCallAsync(_connection, new MethodCallRequestModel
                {
                    ObjectId = Plc.Namespaces.PlcApplications + "#s=Methods",
                    MethodId = Plc.Namespaces.PlcSimulation + "#s=HeaterOn"
                }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Null(result.ErrorInfo);
            }

            async Task TurnHeaterOffAsync()
            {
                var result = await services.MethodCallAsync(_connection, new MethodCallRequestModel
                {
                    ObjectId = Plc.Namespaces.PlcApplications + "#s=Methods",
                    MethodId = Plc.Namespaces.PlcSimulation + "#s=HeaterOff"
                }).ConfigureAwait(false);

                Assert.NotNull(result);
                Assert.Null(result.ErrorInfo);
            }

            async Task<PlcDataType?> GetPlcModelAsync()
            {
                var value = await services.ValueReadAsync(_connection, new ValueReadRequestModel
                {
                    NodeId = Plc.Namespaces.PlcSimulation + "#i=" + Variables.Plc1_PlcStatus
                }).ConfigureAwait(false);

                Assert.NotNull(value);
                Assert.Null(value.ErrorInfo);
                Assert.NotNull(value.Value);
                Assert.True(value.Value.TryGetProperty("Body", out var body));

                // TODO: workaround decoder shortfall.  Need to look into this.
                ISerializer serializer = new NewtonsoftJsonSerializer();
                return serializer.Deserialize<PlcDataType>(serializer.SerializeToMemory(body));
            }
        }

        private readonly T _connection;
        private readonly BaseServerFixture _server;
        private readonly Func<INodeServices<T>> _services;
    }
}
