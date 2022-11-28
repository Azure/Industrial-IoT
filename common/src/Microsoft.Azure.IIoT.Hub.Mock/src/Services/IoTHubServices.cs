// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Mock.SqlParser;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Mock device registry
    /// </summary>
    public class IoTHubServices : IIoTHubTwinServices,
        IIoTHubTelemetryServices, IIoTHub, IEventProcessingHost, IHostProcess {

        /// <inheritdoc/>
        public string HostName { get; } = "mock.azure-devices.net";

        /// <inheritdoc/>
        public IEnumerable<IIoTHubDevice> Devices =>
            _devices.Where(d => d.Device.ModuleId == null);

        /// <inheritdoc/>
        public IEnumerable<IIoTHubDevice> Modules =>
            _devices.Where(d => d.Device.ModuleId != null);

        /// <inheritdoc/>
        public BlockingCollection<EventMessage> Events { get; } =
            new BlockingCollection<EventMessage>();

        /// <summary>
        /// Create iot hub services
        /// </summary>
        /// <param name="config"></param>
        public IoTHubServices(IIoTHubConfig config = null) :
            this(config, null, null) {
        }

        /// <summary>
        /// Create iot hub services
        /// </summary>
        /// <param name="config"></param>
        /// <param name="devices"></param>
        /// <param name="serializer"></param>
        private IoTHubServices(IIoTHubConfig config,
            IEnumerable<(DeviceTwinModel, DeviceModel)> devices,
            IJsonSerializer serializer) {
            if (config?.IoTHubConnString != null) {
                HostName = ConnectionString.Parse(config.IoTHubConnString).HostName;
            }
            if (devices != null) {
                _devices.AddRange(devices
                    .Select(d => new IoTHubDeviceModel(this, d.Item2, d.Item1)));
            }
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
            _query = new SqlQuery(this, _serializer);
        }

        /// <summary>
        /// Create iot hub services with devices
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="devices"></param>
        public static IoTHubServices Create(
            IEnumerable<(DeviceTwinModel, DeviceModel)> devices,
            IJsonSerializer serializer = null) {
            return new IoTHubServices(null, devices, serializer);
        }

        /// <inheritdoc/>
        public Task StartAsync() {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync() {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public IIoTHubConnection Connect(string deviceId, string moduleId,
            IIoTClientCallback callback) {
            var model = GetModel(deviceId, moduleId);
            if (model == null) {
                return null; // Failed to connect.
            }
            if (model.Connection != null) {
                return null; // Already connected
            }
            model.Connect(callback);
            return model;
        }

        /// <inheritdoc/>
        public Task SendAsync(string deviceId, string moduleId, EventModel message) {
            var payload = _serializer.SerializeToBytes(message.Payload).ToArray();
            var ev = new EventMessage {
                DeviceId = deviceId,
                ModuleId = moduleId,
                EnqueuedTimeUtc = DateTime.UtcNow,
                Message = new Message(payload)
            };
            foreach (var item in message.Properties) {
                ev.Message.Properties.Add(item.Key, item.Value);
            }
            Events.TryAdd(ev);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel twin, bool force,
            CancellationToken ct) {
            lock (_lock) {
                var model = GetModel(twin.Id, twin.ModuleId);
                if (model == null) {
                    // Create
                    model = new IoTHubDeviceModel(this,
                        new DeviceModel { Id = twin.Id, ModuleId = twin.ModuleId }, twin);
                    _devices.Add(model);
                }
                else if (!force) {
                    throw new ConflictingResourceException("Twin conflict");
                }
                model.UpdateTwin(twin);
                return Task.FromResult(model.Twin);
            }
        }


        /// <inheritdoc/>
        public Task<DeviceTwinModel> PatchAsync(DeviceTwinModel twin, bool force,
            CancellationToken ct) {
            lock (_lock) {
                var model = GetModel(twin.Id, twin.ModuleId);
                if (model == null) {
                    throw new ResourceNotFoundException("Twin not found");
                }
                model.UpdateTwin(twin);
                return Task.FromResult(model.Twin);
            }
        }

        /// <inheritdoc/>
        public Task<MethodResultModel> CallMethodAsync(string deviceId, string moduleId,
            MethodParameterModel parameters, CancellationToken ct) {
            lock (_lock) {
                var model = GetModel(deviceId, moduleId);
                if (model == null) {
                    throw new ResourceNotFoundException("No such device");
                }
                if (model.Connection == null) {
                    throw new TimeoutException("Timed out waiting for device to connect");
                }
                var result = model.Connection.Call(new MethodRequest(parameters.Name,
                    Encoding.UTF8.GetBytes(parameters.JsonPayload),
                    parameters.ResponseTimeout, parameters.ConnectionTimeout));
                return Task.FromResult(new MethodResultModel {
                    JsonPayload = result.ResultAsJson,
                    Status = result.Status
                });
            }
        }

        /// <inheritdoc/>
        public Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, VariantValue> properties, string etag, CancellationToken ct) {
            lock (_lock) {
                var model = GetModel(deviceId, moduleId, etag);
                if (model == null) {
                    throw new ResourceNotFoundException("No such device");
                }
                model.UpdateDesiredProperties(properties);
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> GetAsync(string deviceId, string moduleId,
            CancellationToken ct) {
            lock (_lock) {
                var model = GetModel(deviceId, moduleId);
                if (model == null) {
                    throw new ResourceNotFoundException("No such device");
                }
                return Task.FromResult(model.Twin.Clone());
            }
        }

        /// <inheritdoc/>
        public Task<DeviceModel> GetRegistrationAsync(string deviceId, string moduleId,
            CancellationToken ct) {
            lock (_lock) {
                var model = GetModel(deviceId, moduleId);
                if (model == null) {
                    throw new ResourceNotFoundException("No such device");
                }
                return Task.FromResult(model.Device.Clone());
            }
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string deviceId, string moduleId, string etag,
            CancellationToken ct) {
            lock (_lock) {
                var model = GetModel(deviceId, moduleId, etag);
                if (model == null) {
                    throw new ResourceNotFoundException("No such device");
                }
                model.Connect(null);
                _devices.RemoveAll(d => d.Device.Id == deviceId && d.Device.ModuleId == moduleId);
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        public Task<QueryResultModel> QueryAsync(string query, string continuation,
            int? pageSize, CancellationToken ct) {
            lock (_lock) {
                var result = _query.Query(query).Select(r => r.Copy()).ToList();
                if (pageSize == null) {
                    pageSize = int.MaxValue;
                }

                _ = int.TryParse(continuation, out var index);
                var count = Math.Max(0, Math.Min(pageSize.Value, result.Count - index));

                return Task.FromResult(new QueryResultModel {
                    ContinuationToken = count >= result.Count ? null : count.ToString(),
                    Result = _serializer.FromObject(result.Skip(index).Take(count)).Values.ToList()
                });
            }
        }

        /// <summary>
        /// Get device model
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="etag"></param>
        /// <returns></returns>
        private IoTHubDeviceModel GetModel(string deviceId, string moduleId,
            string etag = null) {
            var model = _devices.FirstOrDefault(
                t => t.Device.Id == deviceId && t.Device.ModuleId == moduleId);
            if (model != null && etag != null && model.Device.Etag != etag) {
                model = null;
            }
            return model;
        }

        /// <summary>
        /// Storage record for device plus twin
        /// </summary>
        public class IoTHubDeviceModel : IIoTHubDevice, IIoTHubConnection {

            /// <summary>
            /// Device
            /// </summary>
            public DeviceModel Device { get; }

            /// <summary>
            /// Twin model
            /// </summary>
            public DeviceTwinModel Twin { get; }

            /// <summary>
            /// The connected client - only one client can be connected simultaneously
            /// </summary>
            public IIoTClientCallback Connection { get; set; }

            /// <summary>
            /// Create device
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="device"></param>
            /// <param name="twin"></param>
            public IoTHubDeviceModel(IoTHubServices outer,
                DeviceModel device, DeviceTwinModel twin) {
                _outer = outer;

                Device = device.Clone();
                Twin = twin.Clone();

                // Simulate authentication
                if (Device.Authentication == null) {
                    Device.Authentication = new DeviceAuthenticationModel {
                        PrimaryKey = Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())),
                        SecondaryKey = Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))
                    };
                }
                if (Device.ConnectionState == null) {
                    Device.ConnectionState = "Disconnected";
                }
                if (Twin.ConnectionState == null) {
                    Twin.ConnectionState = "Disconnected";
                }
                if (Twin.Status == null) {
                    Twin.Status = "enabled";
                }
                if (Twin.StatusUpdatedTime == null) {
                    Twin.StatusUpdatedTime = DateTime.UtcNow;
                }
                if (Twin.LastActivityTime == null) {
                    Twin.LastActivityTime = DateTime.UtcNow;
                }
                Twin.Etag = Device.Etag = Guid.NewGuid().ToString();
            }

            /// <inheritdoc/>
            public void Close() {
                Connect(null);
            }

            /// <inheritdoc/>
            public MethodResponse Call(string deviceId, string moduleId,
                MethodRequest methodRequest) {
                var response = _outer.CallMethodAsync(deviceId, moduleId,
                    new MethodParameterModel {
                        JsonPayload = methodRequest.DataAsJson,
                        Name = methodRequest.Name,
                        ConnectionTimeout = methodRequest.ConnectionTimeout,
                        ResponseTimeout = methodRequest.ResponseTimeout
                    }, CancellationToken.None).Result;
                return new MethodResponse(Encoding.UTF8.GetBytes(
                    response.JsonPayload), response.Status);
            }

            /// <inheritdoc/>
            public Twin GetTwin() {
                lock (_lock) {
                    var twin = Twin.Clone();

                    // Wipe out what a client should not see...
                    twin.Tags = null;
                    twin.ConnectionState = null;
                    twin.Status = null;

                    if (twin.Properties == null) {
                        twin.Properties = new TwinPropertiesModel();
                    }
                    if (twin.Properties.Reported == null) {
                        twin.Properties.Reported = new Dictionary<string, VariantValue>();
                    }
                    if (twin.Properties.Desired == null) {
                        twin.Properties.Desired = new Dictionary<string, VariantValue>();
                    }
                    // Double clone but that is ok.
                    return twin.ToTwin(false);
                }
            }

            /// <inheritdoc/>
            public void SendBlob(string blobName, ArraySegment<byte> blob) {
                if (!_outer._blobs.TryAdd(new FileNotification {
                    DeviceId = Device.Id,
                    BlobName = blobName,
                    Blob = blob,
                    EnqueuedTimeUtc = DateTime.UtcNow
                })) {
                    throw new CommunicationException("Failed to upload blob.");
                }
            }

            /// <inheritdoc/>
            public void SendEvent(Message message) {
                if (!_outer.Events.TryAdd(new EventMessage {
                    DeviceId = Device.Id,
                    ModuleId = Device.ModuleId,
                    Message = message,
                    EnqueuedTimeUtc = DateTime.UtcNow
                })) {
                    throw new CommunicationException("Failed to send event.");
                }
            }

            /// <inheritdoc/>
            public void SendEvent(string outputName, Message message) {
                if (!_outer.Events.TryAdd(new EventMessage {
                    DeviceId = Device.Id,
                    ModuleId = Device.ModuleId,
                    Message = message,
                    EnqueuedTimeUtc = DateTime.UtcNow,
                    OutputName = outputName
                })) {
                    throw new CommunicationException("Failed to send event.");
                }
            }

            /// <inheritdoc/>
            public void UpdateReportedProperties(TwinCollection reportedProperties) {
                lock (_lock) {
                    if (Twin.Properties == null) {
                        Twin.Properties = new TwinPropertiesModel();
                    }
                    Twin.Properties.Reported = Merge(Twin.Properties.Reported,
                        _outer._serializer.DeserializeTwinProperties(reportedProperties));
                    Twin.LastActivityTime = DateTime.UtcNow;
                    Twin.Etag = Device.Etag = Guid.NewGuid().ToString();
                }
            }

            /// <summary>
            /// Update desired properties
            /// </summary>
            /// <param name="properties"></param>
            public void UpdateDesiredProperties(Dictionary<string, VariantValue> properties) {
                lock (_lock) {
                    if (Twin.Properties == null) {
                        Twin.Properties = new TwinPropertiesModel();
                    }
                    Twin.Properties.Desired = Merge(Twin.Properties.Desired,
                        properties);
                    Twin.LastActivityTime = DateTime.UtcNow;
                    Twin.Etag = Device.Etag = Guid.NewGuid().ToString();
                }
                if (Connection != null) {
                    var desired = new TwinCollection();
                    foreach (var item in Twin.Properties.Desired) {
                        desired[item.Key] = item.Value?.ConvertTo<object>();
                    }
                    Connection.SetDesiredProperties(desired);
                }
            }

            /// <summary>
            /// Update twin
            /// </summary>
            /// <param name="twin"></param>
            internal void UpdateTwin(DeviceTwinModel twin) {
                lock (_lock) {
                    Twin.Tags = Merge(Twin.Tags, twin.Tags);
                    if (Twin.Properties == null) {
                        Twin.Properties = new TwinPropertiesModel();
                    }
                    Twin.Properties.Desired = Merge(
                        Twin.Properties.Desired, twin.Properties?.Desired);
                    Twin.Properties.Reported = Merge(
                        Twin.Properties.Reported, twin.Properties?.Reported);
                    Twin.LastActivityTime = DateTime.UtcNow;
                    Twin.Etag = Device.Etag = Guid.NewGuid().ToString();
                }
            }

            /// <summary>
            /// Connect or disconnect client
            /// </summary>
            /// <param name="client"></param>
            public void Connect(IIoTClientCallback client) {
                lock (_lock) {
                    Connection = client;
                    Twin.ConnectionState = Device.ConnectionState =
                        client == null ? "Disconnected" : "Connected";
                }
            }

            /// <summary>
            /// Merge properties
            /// </summary>
            /// <param name="target"></param>
            /// <param name="source"></param>
            private Dictionary<string, VariantValue> Merge(
                Dictionary<string, VariantValue> target,
                Dictionary<string, VariantValue> source) {

                if (source == null) {
                    return target;
                }

                if (target == null) {
                    return source;
                }

                foreach (var item in source) {
                    if (target.ContainsKey(item.Key)) {
                        if (VariantValueEx.IsNull(item.Value) || VariantValueEx.IsNull(item.Value)) {
                            target.Remove(item.Key);
                        }
                        else {
                            target[item.Key] = item.Value;
                        }
                    }
                    else if (!VariantValueEx.IsNull(item.Value)) {
                        target.Add(item.Key, item.Value);
                    }
                }
                return target;
            }

            private readonly IoTHubServices _outer;
            private readonly object _lock = new object();
        }

        /// <summary>
        /// File notification
        /// </summary>
        public class FileNotification {
            /// <summary/>
            public string DeviceId { get; set; }
            /// <summary/>
            public ArraySegment<byte> Blob { get; set; }
            /// <summary/>
            public string BlobName { get; set; }
            /// <summary/>
            public DateTime EnqueuedTimeUtc { get; set; }
        }

        /// <summary>
        /// Event messages
        /// </summary>
        public class EventMessage {
            /// <summary/>
            public string DeviceId { get; set; }
            /// <summary/>
            public string ModuleId { get; set; }
            /// <summary/>
            public DateTime EnqueuedTimeUtc { get; set; }
            /// <summary/>
            public Message Message { get; set; }
            /// <summary> The output target for sending the given message. </summary>
            public string OutputName { get; set; }
        }

        private readonly SqlQuery _query;
        private readonly object _lock = new object();
        private readonly BlockingCollection<FileNotification> _blobs =
            new BlockingCollection<FileNotification>();
        private readonly List<IoTHubDeviceModel> _devices =
            new List<IoTHubDeviceModel>();
        private readonly IJsonSerializer _serializer;
    }
}
