// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using SharpPcap;
using SharpPcap.LibPcap;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System;

/// <summary>
/// Pcap capture
/// </summary>
internal sealed class Pcap : IDisposable
{
    /// <summary>
    /// Pcap configuration
    /// </summary>
    public sealed record class CaptureConfiguration(string File,
        InterfaceType InterfaceType, string? Filter = null);

    public enum InterfaceType
    {
        AnyIfAvailable,
        AllButAny,
        EthernetOnly
    }

    /// <summary>
    /// Start of capture
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// End of capture
    /// </summary>
    public DateTimeOffset? End { get; private set; }

    /// <summary>
    /// File or handle
    /// </summary>
    public string File => _configuration.File;

    /// <summary>
    /// Pcap file is capturing
    /// </summary>
    public bool Open => End == null;

    /// <summary>
    /// Remote capture
    /// </summary>
    public bool Remote => _hostCaptureEndpointUrl != null;

    /// <summary>
    /// Create pcap
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hostCaptureEndpointUrl"></param>
    /// <param name="logger"></param>
    public Pcap(ILogger logger, CaptureConfiguration configuration,
        string? hostCaptureEndpointUrl = null)
    {
        _configuration = configuration;
        _hostCaptureEndpointUrl = hostCaptureEndpointUrl;
        _logger = logger;

        if (_hostCaptureEndpointUrl != null)
        {
            // TODO: Remote the capture start/stop operation
            throw new NotSupportedException("Remote capture not supported yet.");
        }

        _logger.LogInformation(
            "Using SharpPcap {Version}", SharpPcap.Pcap.SharpPcapVersion);

        if (LibPcapLiveDeviceList.Instance.Count == 0)
        {
            throw new NetcapException("Cannot run capture without devices.");
        }
        else
        {
            _writer = new CaptureFileWriterDevice(File);

            _devices = LibPcapLiveDeviceList.New().ToList();
            // Open devices
            var open = _devices
                .Where(d =>
                {
                    try
                    {
                        _logger.LogInformation("Opening {Device} in promiscuous mode...", d);
                        d.Open(mode: DeviceModes.Promiscuous, 1000);
                    }
                    catch
                    {
                        try
                        {
                            _logger.LogInformation("Fall back to normal mode...");
                            d.Open(mode: DeviceModes.None, 1000);
                        }
                        catch (Exception ex2)
                        {
                            _logger.LogInformation(
                                "Failed to open {Device} ({Description}): {Message}",
                                d.Name, d.Description, ex2.Message);
                        }
                    }
                    return d.Opened;
                })
                .ToList();

            var capturing = Array.Empty<LibPcapLiveDevice>();
            var linkType = PacketDotNet.LinkLayers.Null;
            var itf = _configuration.InterfaceType;
            if (itf == InterfaceType.AnyIfAvailable)
            {
                // Try to capture from cooked mode (https://wiki.wireshark.org/SLL)
                linkType = PacketDotNet.LinkLayers.LinuxSll;
                capturing = Capture(open.Where(d => d.LinkType == linkType));
                if (capturing.Length == 0)
                {
                    itf = InterfaceType.AllButAny;
                }
            }
            if (itf == InterfaceType.EthernetOnly)
            {
                linkType = PacketDotNet.LinkLayers.Ethernet;
                capturing = Capture(open.Where(d => d.LinkType != linkType));
                if (capturing.Length == 0)
                {
                    itf = InterfaceType.AllButAny;
                }
            }
            if (itf == InterfaceType.AllButAny)
            {
                linkType = PacketDotNet.LinkLayers.Null;
                capturing = Capture(open.Where(d =>
                    d.LinkType != PacketDotNet.LinkLayers.LinuxSll));
            }

            if (capturing.Length == 0)
            {
                // Capture from all interfaces that are open
                linkType = PacketDotNet.LinkLayers.Null;
                capturing = Capture(open);
            }

            _writer.Open(new DeviceConfiguration
            {
                LinkLayerType = linkType
            });

            if (capturing.Length != 0)
            {
                foreach (var device in capturing)
                {
                    device.StartCapture();
                    _logger.LogInformation("Capturing {Device} ({Description})...",
                        device.Name, device.Description);
                };
                _logger.LogInformation("    ... to {FileName} ({Filter}).",
                    File, _configuration.Filter ?? "No filter");
            }
            else
            {
                _logger.LogWarning("No capture devices found to capture from.");
            }

            LibPcapLiveDevice[] Capture(IEnumerable<LibPcapLiveDevice> candidates)
            {
                var capturing = new List<LibPcapLiveDevice>();
                foreach (var device in candidates)
                {
                    try
                    {
                        // Open the device for capturing
                        Debug.Assert(device.Opened);
                        if (_configuration.Filter != null)
                        {
                            device.Filter = _configuration.Filter;
                        }
                        device.OnPacketArrival += (_, e) => _writer.Write(e.GetPacket());

                        capturing.Add(device);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to capture {Device} ({Description}): {Message}",
                            device.Name, device.Description, ex.Message);
                    }
                }
                return capturing.ToArray();
            }
        }
        Start = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!Open)
        {
            return;
        }

        if (_hostCaptureEndpointUrl != null)
        {
            // TODO: Remote the capture start/stop operation
            throw new NotSupportedException("Remote capture not supported yet.");
        }
        else
        {
            try
            {
                End = DateTimeOffset.UtcNow;
                _devices?.ForEach(d =>
                {
                    try
                    {
                        d.StopCapture();
                        _logger.LogInformation(
                            "Capturing {Description} completed. ({Statistics}).",
                            d.Description, d.Statistics.ToString());
                    }
                    catch { }
                });
                if (_writer != null)
                {
                    _logger.LogInformation("Completed capture ({Statistics}).",
                        _writer.Statistics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete capture.");
                throw;
            }
            finally
            {
                _writer?.Dispose();
                _devices?.ForEach(d => d.Dispose());
            }
        }
    }

    private readonly CaptureConfiguration _configuration;
    private readonly string? _hostCaptureEndpointUrl;
    private readonly List<LibPcapLiveDevice>? _devices;
    private readonly CaptureFileWriterDevice? _writer;
    private readonly ILogger _logger;
}
