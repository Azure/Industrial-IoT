// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport;
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models;
    using System.Net.NetworkInformation;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="NetworkInformationEx.IsInClass"/>.
    /// </summary>
    public class NetworkInformationIsInClassTests
    {
        // ─────────────────────── Wired ───────────────────────

        [Fact]
        public void Ethernet_IsInClass_Wired_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Ethernet.IsInClass(NetworkClass.Wired));

        [Fact]
        public void Ethernet3Megabit_IsInClass_Wired_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Ethernet3Megabit.IsInClass(NetworkClass.Wired));

        [Fact]
        public void GigabitEthernet_IsInClass_Wired_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.GigabitEthernet.IsInClass(NetworkClass.Wired));

        [Fact]
        public void FastEthernetT_IsInClass_Wired_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.FastEthernetT.IsInClass(NetworkClass.Wired));

        [Fact]
        public void FastEthernetFx_IsInClass_Wired_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.FastEthernetFx.IsInClass(NetworkClass.Wired));

        [Fact]
        public void Slip_IsInClass_Wired_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Slip.IsInClass(NetworkClass.Wired));

        [Fact]
        public void IPOverAtm_IsInClass_Wired_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.IPOverAtm.IsInClass(NetworkClass.Wired));

        [Fact]
        public void Ethernet_IsInClass_Modem_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.Ethernet.IsInClass(NetworkClass.Modem));

        [Fact]
        public void Ethernet_IsInClass_All_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Ethernet.IsInClass(NetworkClass.All));

        [Fact]
        public void Ethernet_IsInClass_None_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.Ethernet.IsInClass(NetworkClass.None));

        // ─────────────────────── Modem ───────────────────────

        [Fact]
        public void BasicIsdn_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.BasicIsdn.IsInClass(NetworkClass.Modem));

        [Fact]
        public void PrimaryIsdn_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.PrimaryIsdn.IsInClass(NetworkClass.Modem));

        [Fact]
        public void Isdn_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Isdn.IsInClass(NetworkClass.Modem));

        [Fact]
        public void GenericModem_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.GenericModem.IsInClass(NetworkClass.Modem));

        [Fact]
        public void AsymmetricDsl_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.AsymmetricDsl.IsInClass(NetworkClass.Modem));

        [Fact]
        public void SymmetricDsl_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.SymmetricDsl.IsInClass(NetworkClass.Modem));

        [Fact]
        public void RateAdaptDsl_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.RateAdaptDsl.IsInClass(NetworkClass.Modem));

        [Fact]
        public void VeryHighSpeedDsl_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.VeryHighSpeedDsl.IsInClass(NetworkClass.Modem));

        [Fact]
        public void MultiRateSymmetricDsl_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.MultiRateSymmetricDsl.IsInClass(NetworkClass.Modem));

        [Fact]
        public void Ppp_IsInClass_Modem_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Ppp.IsInClass(NetworkClass.Modem));

        [Fact]
        public void GenericModem_IsInClass_Wired_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.GenericModem.IsInClass(NetworkClass.Wired));

        [Fact]
        public void GenericModem_IsInClass_All_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.GenericModem.IsInClass(NetworkClass.All));

        // ─────────────────────── Wireless ───────────────────────

        [Fact]
        public void Wireless80211_IsInClass_Wireless_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Wireless80211.IsInClass(NetworkClass.Wireless));

        [Fact]
        public void Wman_IsInClass_Wireless_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Wman.IsInClass(NetworkClass.Wireless));

        [Fact]
        public void Wwanpp_IsInClass_Wireless_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Wwanpp.IsInClass(NetworkClass.Wireless));

        [Fact]
        public void Wwanpp2_IsInClass_Wireless_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Wwanpp2.IsInClass(NetworkClass.Wireless));

        [Fact]
        public void Wireless80211_IsInClass_Wired_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.Wireless80211.IsInClass(NetworkClass.Wired));

        [Fact]
        public void Wireless80211_IsInClass_All_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Wireless80211.IsInClass(NetworkClass.All));

        // ─────────────────────── Tunnel ───────────────────────

        [Fact]
        public void Tunnel_IsInClass_Tunnel_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Tunnel.IsInClass(NetworkClass.Tunnel));

        [Fact]
        public void Tunnel_IsInClass_Wired_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.Tunnel.IsInClass(NetworkClass.Wired));

        [Fact]
        public void Tunnel_IsInClass_All_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Tunnel.IsInClass(NetworkClass.All));

        // ─────────────────────── Excluded types ───────────────────────

        [Fact]
        public void TokenRing_IsInClass_All_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.TokenRing.IsInClass(NetworkClass.All));

        [Fact]
        public void HighPerformanceSerialBus_IsInClass_All_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.HighPerformanceSerialBus.IsInClass(NetworkClass.All));

        [Fact]
        public void Fddi_IsInClass_All_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.Fddi.IsInClass(NetworkClass.All));

        [Fact]
        public void Atm_IsInClass_All_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.Atm.IsInClass(NetworkClass.All));

        [Fact]
        public void Loopback_IsInClass_All_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.Loopback.IsInClass(NetworkClass.All));

        [Fact]
        public void Unknown_IsInClass_All_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.Unknown.IsInClass(NetworkClass.All));

        // ─────────────────────── Combined flags ───────────────────────

        [Fact]
        public void Ethernet_IsInClass_WiredOrWireless_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Ethernet.IsInClass(NetworkClass.Wired | NetworkClass.Wireless));

        [Fact]
        public void Wireless80211_IsInClass_WiredOrWireless_ReturnsTrue() =>
            Assert.True(NetworkInterfaceType.Wireless80211.IsInClass(NetworkClass.Wired | NetworkClass.Wireless));

        [Fact]
        public void GenericModem_IsInClass_WiredOrWireless_ReturnsFalse() =>
            Assert.False(NetworkInterfaceType.GenericModem.IsInClass(NetworkClass.Wired | NetworkClass.Wireless));
    }
}
