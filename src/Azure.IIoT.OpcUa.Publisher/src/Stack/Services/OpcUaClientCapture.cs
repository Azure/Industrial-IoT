// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using SharpPcap;
    using SharpPcap.LibPcap;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using Autofac;

    /// <summary>
    /// Opc Ua traffic capture capabilities
    /// </summary>
    public sealed class OpcUaClientCapture : IStartable, IDisposable
    {
        /// <summary>
        /// Get the devices that can be used to capture
        /// </summary>
        public static IReadOnlyList<string> AvailableDevices
        {
            get
            {
                try
                {
                    return LibPcapLiveDeviceList.Instance
                        .Select(d => d.Interface.FriendlyName ?? d.Name)
                        .ToArray();
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }
        }

        /// <summary>
        /// Create capture service
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public OpcUaClientCapture(IOptions<OpcUaClientOptions> options,
            ILogger<OpcUaClientCapture> logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Start()
        {
            // Find device
            var deviceName = _options.Value.CaptureDevice;
            if (string.IsNullOrEmpty(deviceName))
            {
                return;
            }

            _logger.LogInformation("Using SharpPcap {Version}", Pcap.SharpPcapVersion);
            var device = FindDeviceByName(deviceName);
            if (device == null)
            {
                _logger.LogError("Could not find a capture device with name {Name}! " +
                    "Not capturing traffic...", deviceName);
                return;
            }

            _device?.Dispose();
            _device = new CaptureDevice(this, device, _options.Value.CaptureFileName);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _device?.Dispose();
            _device = null;
        }

        /// <summary>
        /// Find device by name
        /// </summary>
        /// <param name="deviceName"></param>
        /// <returns></returns>
        private static LibPcapLiveDevice? FindDeviceByName(string deviceName)
        {
            var device = LibPcapLiveDeviceList.Instance
                .FirstOrDefault(d => d.Interface.FriendlyName == deviceName);
            if (device == null)
            {
                device = LibPcapLiveDeviceList.Instance
                    .FirstOrDefault(d => d.Name == deviceName);
                if (device == null && deviceName == "loopback")
                {
                    device = LibPcapLiveDeviceList.Instance
                        .FirstOrDefault(d => d.Loopback);
                }
            }
            return device;
        }

        /// <summary>
        /// Capture device
        /// </summary>
        private sealed class CaptureDevice : IDisposable
        {
            /// <summary>
            /// Create capture device
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="device"></param>
            /// <param name="fileName"></param>
            public CaptureDevice(OpcUaClientCapture outer,
                LibPcapLiveDevice device, string? fileName = null)
            {
                _outer = outer;
                _device = device;

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "opcua.pcap";
                }

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                _outer._logger.LogInformation(
                    "Start capturing {Device} ({Description}) to {FileName}.",
                    device.Name, device.Description, fileName);

                // Open the device for capturing
                _device.Open(mode: DeviceModes.NoCaptureLocal, 1000);
                // _device.Filter = "ip and tcp and not port 80 and not port 25";

                _writer = new CaptureFileWriterDevice(fileName);
                _writer.Open(_device);
                _device.OnPacketArrival += (_, e) => _writer.Write(e.GetPacket());

                // Start the capturing process
                _device.StartCapture();
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                try
                {
                    _device.StopCapture();

                    _outer._logger.LogInformation(
                        "Stopped capturing {Device} ({Description}) to file ({Statistics}).",
                        _device.Name, _device.Description, _device.Statistics.ToString());

                    _writer.Close();
                }
                finally
                {
                    _writer.Dispose();
                    _device.Dispose();
                }
            }

            private readonly LibPcapLiveDevice _device;
            private readonly CaptureFileWriterDevice _writer;
            private readonly OpcUaClientCapture _outer;
        }

        private CaptureDevice? _device;
        private readonly IOptions<OpcUaClientOptions> _options;
        private readonly ILogger<OpcUaClientCapture> _logger;
    }
}
