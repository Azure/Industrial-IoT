// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using global::IoTHubby;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Xunit;

    internal sealed class IoTEdgeTestModuleClient : IIoTHubModuleClient
    {
        public IoTHubConnectionState State { get; set; }

        public int ConnectCount { get; private set; }

        public int DisposeCount { get; private set; }

        public List<TelemetryMessage> Telemetry { get; } = [];

        public List<(string Output, TelemetryMessage Message)> OutputTelemetry { get; } = [];

        public Channel<CloudToDeviceMessage> Inputs { get; } =
            Channel.CreateUnbounded<CloudToDeviceMessage>();

        public Func<DirectMethodRequest, CancellationToken,
            ValueTask<DirectMethodResponse>>? MethodHandler { get; private set; }

        public Twin Twin { get; set; } = CreateTwin("{}", "{}");

        public Channel<string> ReportedPropertyPatches { get; } =
            Channel.CreateUnbounded<string>();

        public Exception? SendException { get; set; }

        public Exception? SetMethodHandlerException { get; set; }

        public event EventHandler<IoTHubConnectionStateChangedEventArgs>?
            ConnectionStateChanged;

        public Task ConnectAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ConnectCount++;
            State = IoTHubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public ValueTask SendTelemetryAsync(TelemetryMessage message, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (SendException != null)
            {
                throw SendException;
            }
            Telemetry.Add(message);
            return ValueTask.CompletedTask;
        }

        public ValueTask SendToOutputAsync(string outputName, TelemetryMessage message,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (SendException != null)
            {
                throw SendException;
            }
            OutputTelemetry.Add((outputName, message));
            return ValueTask.CompletedTask;
        }

        public IAsyncEnumerable<CloudToDeviceMessage> ReceiveInputMessagesAsync(
            string inputName, CancellationToken ct)
        {
            Assert.Equal(string.Empty, inputName);
            return Inputs.Reader.ReadAllAsync(ct);
        }

        public Task SetMethodHandlerAsync(Func<DirectMethodRequest, CancellationToken,
            ValueTask<DirectMethodResponse>>? handler, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (SetMethodHandlerException != null)
            {
                throw SetMethodHandlerException;
            }
            MethodHandler = handler;
            return Task.CompletedTask;
        }

        public Task<Twin> GetTwinAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Twin);
        }

        public async Task<long?> UpdateReportedPropertiesAsync(string json,
            CancellationToken ct)
        {
            await ReportedPropertyPatches.Writer.WriteAsync(json, ct).ConfigureAwait(false);
            return 1;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }

        public void RaiseStateChanged(IoTHubConnectionState state, string? reason = null)
        {
            State = state;
            ConnectionStateChanged?.Invoke(this,
                Create<IoTHubConnectionStateChangedEventArgs>(state, reason!));
        }

        public static Twin CreateTwin(string desiredJson, string reportedJson)
        {
            return Create<Twin>(
                Create<TwinProperties>(Encoding.UTF8.GetBytes(desiredJson), null),
                Create<TwinProperties>(Encoding.UTF8.GetBytes(reportedJson), null));
        }

        public static CloudToDeviceMessage CreateInputMessage(byte[] payload,
            IReadOnlyDictionary<string, string> systemProperties,
            IReadOnlyDictionary<string, string> properties)
        {
            ReadOnlyMemory<byte> payloadMemory = payload;
            return Create<CloudToDeviceMessage>(payloadMemory, systemProperties,
                properties);
        }

        public static DirectMethodRequest CreateDirectMethodRequest(string name,
            ReadOnlySequence<byte> payload)
        {
            return Create<DirectMethodRequest>(name, payload);
        }

        private static T Create<T>(params object?[] args)
        {
            // IoTHubby exposes these DTOs publicly but keeps their constructors internal.
            return (T)Activator.CreateInstance(typeof(T),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, args, culture: null)!;
        }
    }

    internal sealed class IoTEdgeTestModuleClientFactory :
        IIoTHubModuleClientFactory
    {
        public IoTEdgeTestModuleClientFactory(IoTEdgeTestModuleClient client)
        {
            Client = client;
        }

        public IoTEdgeTestModuleClient Client { get; }

        public IoTEdgeClientOptions? Options { get; private set; }

        public global::IoTHubby.IoTHubClientOptions? ConfiguredOptions { get; private set; }

        public IIoTHubModuleClient Create(IoTEdgeClientOptions options,
            Action<global::IoTHubby.IoTHubClientOptions> configure)
        {
            Options = options;
            ConfiguredOptions = new global::IoTHubby.IoTHubClientOptions();
            configure(ConfiguredOptions);
            return Client;
        }
    }

    internal sealed class IoTEdgeTestIdentity : IIoTEdgeDeviceIdentity
    {
        public string? Hub => "hub";
        public string DeviceId => "device";
        public string? ModuleId { get; init; } = "module";
        public string? Gateway { get; init; }
    }
}
