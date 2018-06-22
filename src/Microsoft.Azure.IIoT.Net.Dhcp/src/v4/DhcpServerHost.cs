// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.v4 {
    using Microsoft.Azure.IIoT.Net.Dhcp.Shared;
    using Microsoft.Azure.IIoT.Net.Dhcp;
    using Microsoft.Azure.IIoT.Net.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of dhcp server. Implements multiple operational modes:
    /// - Active: Full server - cooperatively service offers and requests
    /// - Passive: Only listen and record conversations (TODO)
    /// - Hybrid: First listen, and if request is re-sent, service request (TODO)
    /// </summary>
    public class DhcpServerHost : IDhcpServerHost, IDhcpResponder {

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="logger"></param>
        public DhcpServerHost(IDhcpServer protocol, ILogger logger,
            IDhcpServerConfig configuration = null) {
            _configuration = configuration;
            _protocol = protocol ??
                throw new ArgumentNullException(nameof(protocol));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Stop server
        /// </summary>
        public async Task StopAsync() {
            await _lock.WaitAsync();
            try {
                if (_cancel == null) {
                    return;
                }
                _cancel.Cancel();
                _timer.Dispose();

                _socket.SafeDispose();

                await _tcs.Task;
                _logger.Info("Dhcp server stopped.", () => { });
            }
            finally {
                _arg?.Dispose();
                _cancel = null;
                _socket = null;
                _lock.Release();
            }
        }

        /// <summary>
        /// Start server
        /// </summary>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_cancel != null) {
                    throw new InvalidOperationException("Already running");
                }

                _cancel = new CancellationTokenSource();
                _tcs = new TaskCompletionSource<bool>();

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                    ProtocolType.Udp);

                _socket.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.PacketInformation, true);
                _socket.SetSocketOption(SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress, true);
                _socket.SetSocketOption(SocketOptionLevel.Socket,
                    SocketOptionName.Broadcast, true);

                // Bind to all network interfaces
                _socket.Bind(new IPEndPoint(IPAddress.Any, kDhcpServerPort));

                _arg = new SocketAsyncEventArgs {
                    // Receive from any client and broadcast
                    RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0),
                };

                _arg.SetBuffer(new byte[kDhcpMessageMaxSize], 0, kDhcpMessageMaxSize);
                _arg.Completed += (_, arg) => OnEndReceive(arg);

                 var x = Task.Run(() => OnBeginReceive(_arg));

                _timer = new Timer(OnTimer, null, 60000, 30000);
                _logger.Info("Dhcp server started.", () => { });
            }
            finally {
                _lock.Release();
            }
        }

        private const int kDhcpServerPort = 67;
        private const int kDhcpClientPort = 68;
        private const int kDhcpMessageMaxSize = 1024;

        /// <summary>
        /// Helper to send response message
        /// </summary>
        /// <param name="address"></param>
        /// <param name="packet"></param>
        /// <param name="length"></param>
        public async Task SendResponseAsync(byte[] packet, int length,
            IPAddress address) {
            try {
                var target = new IPEndPoint(address, kDhcpClientPort);
                await _socket.SendToAsync(packet, 0, length, SocketFlags.None,
                    target);
            }
            catch (Exception ex) {
                _logger.Error("Error sending dhcp response", () => ex);
                throw;
            }
        }

        /// <summary>
        /// Checks whether the address is assigned
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<bool> IsAddressAssignedAsync(IPAddress address) {
            var reply = await new Ping().SendPingAsync(address, 1000);
            return reply.Status == IPStatus.Success;
        }

        /// <inheritdoc/>
        public void Dispose() => StopAsync().Wait();

        /// <summary>
        /// Trim contexts
        /// </summary>
        /// <param name="state"></param>
        private void OnTimer(object state) {
            _lock.Wait();
            try {
                foreach (var lease in _subnets.Values.ToList()) {
                    if (!lease.Trim()) {
                        _subnets.Remove(lease.Interface);
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Begin receive
        /// </summary>
        private void OnBeginReceive(SocketAsyncEventArgs arg) {
            while (!_cancel.IsCancellationRequested) {
                try {
                    if (!_socket.ReceiveMessageFromAsync(arg)) {
                        // Complete
                        OnEndReceive(arg);
                    }
                    // Wait
                    return;
                }
                catch (Exception ex) {
                    _logger.Debug("Exception during begin", () => ex);
                }
            }
            // Done, exit
            _tcs.TrySetResult(true);
        }

        /// <summary>
        /// Process received data
        /// </summary>
        /// <param name="arg"></param>
        private async void OnEndReceive(SocketAsyncEventArgs arg) {
            try {
                if (_cancel.IsCancellationRequested) {
                    _tcs.TrySetResult(true);
                    return;
                }

                var subnet = LookupSubnetContext(
                    arg.ReceiveMessageFromPacketInfo.Interface);
                var buf = _arg.Buffer.Subset(0, _arg.BytesTransferred);
                await _protocol.ProcessMessageAsync(subnet, buf, buf.Length,
                    this);
            }
            catch (Exception ex) {
                _logger.Debug("Exception during receive", () => ex);
            }
            OnBeginReceive(arg);
        }

        /// <summary>
        /// Retrieve the network interface context for the message. This is
        /// not overly performant, however, it is easier to let the stack
        /// bind the socket to all interfaces, and then allocate a context
        /// on demand when a packet is received than finding a platform
        /// independent way to watch coming and going network interfaces.
        /// Our timer callback will cleanup unused network contexts (i.e.
        /// contexts that have no address leases assigned on them).
        /// </summary>
        /// <param name="itfIndex"></param>
        /// <returns></returns>
        private DhcpScope LookupSubnetContext(int itfIndex) {

            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType.IsInClass(NetworkClass.All))
                .Where(n => n.Supports(NetworkInterfaceComponent.IPv4));
            var nic = interfaces.FirstOrDefault(n =>
                n.GetIPProperties().GetIPv4Properties().Index == itfIndex);
            var props = nic.GetIPProperties();
            var address = props.UnicastAddresses.FirstOrDefault(a =>
                a.Address.AddressFamily == AddressFamily.InterNetwork);
            var gateway = props.GatewayAddresses.FirstOrDefault(a =>
                a.Address.AddressFamily == AddressFamily.InterNetwork);

            var key = new NetInterface(nic.Name, nic.GetPhysicalAddress(),
                address.Address, address.IPv4Mask, gateway?.Address,
                props.DnsSuffix, props.DnsAddresses);
            _lock.Wait();
            try {
                if (!_subnets.TryGetValue(key, out var subnet)) {
                    subnet = new DhcpScope(key,
                        _configuration?.AddressRangeOffsetBottom ?? 0,
                        _configuration?.AddressRangeOffsetTop ?? 0,
                        _logger,
                        _configuration?.StaticAssignment.Values.ToHashSetSafe(),
                        _configuration?.AssignDescending ?? false);
                    _subnets.Add(key, subnet);
                }
                return subnet;
            }
            finally {
                _lock.Release();
            }
        }

        private readonly Dictionary<NetInterface, DhcpScope> _subnets =
            new Dictionary<NetInterface, DhcpScope>();
        private readonly SemaphoreSlim _lock =
            new SemaphoreSlim(1);

        private Socket _socket;
        private SocketAsyncEventArgs _arg;
        private CancellationTokenSource _cancel;
        private TaskCompletionSource<bool> _tcs;
        private Timer _timer;

        private readonly IDhcpServerConfig _configuration;
        private readonly IDhcpServer _protocol;
        private readonly ILogger _logger;
    }
}
