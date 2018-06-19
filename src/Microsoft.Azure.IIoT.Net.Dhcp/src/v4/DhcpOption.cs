// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp.v4 {

    /// <summary>
    /// Dhcp option enumreation
    /// </summary>
    public enum DhcpOption : byte {

        // Rfc 1497 begin
        End = 255,                              // No data
        Pad = 0,                                // No data

        SubnetMask,                             // 4 byte
        TimeOffset,                             // 4 byte
        Router,                                 // variable ascii
        TimeServer,                             // variable ascii
        NameServer,                             // variable ascii
        DomainNameServers,                      // variable ascii
        LogServer,                              // variable ascii
        CookieServer,                           // variable ascii
        LPRServer,                              // variable ascii
        ImpressServer,                          // variable ascii
        ResourceLocServer,                      // variable ascii
        HostName,                               // variable ascii
        BootFileSize,                           // variable ascii
        MeritDump,                              // variable ascii
        DomainName,                             // variable ascii
        SwapServer,                             // variable ascii
        RootPath,                               // variable ascii
        ExtensionsPath,                         // variable ascii
        // Rfc 1497 end

        // Rfc 1533 4 - Host IP begin
        IpForwardingEnableDisable,              // 1 byte = bool
        NonLocalSourceRoutingEnableDisable,     // 1 byte = bool
        PolicyFilter,                           // 1..n * 8 byte
        MaximumDatagramReAssemblySize,          // 1 ushort
        DefaultIPTimeToLive,                    // 1 byte
        PathMTUAgingTimeout,                    // 1 uint
        PathMTUPlateauTable,                    // 1..n * ushort
        // Rfc 1533 4 - Host IP end

        // Rfc 1533 5 - Interface IP begin
        InterfaceMTU,                           // 1 ushort
        AllSubnetsAreLocal,                     // 1 byte = bool
        BroadcastAddress,                       // 4 byte
        PerformMaskDiscovery,                   // 1 byte = bool
        MaskSupplier,                           // 1 byte = bool
        PerformRouterDiscovery,                 // 1 byte = bool
        RouterSolicitationAddress,              // 4 byte
        StaticRoute,                            // 1..n * 4+4 byte
        // Rfc 1533 5 - Interface IP end

        // Rfc 1533 6 - Interface L2 begin
        TrailerEncapsulation,                   // 1 byte = bool
        ARPCacheTimeout,                        // 1 uint (secs)
        EthernetEncapsulation,                  // 1 byte = bool
        // Rfc 1533 6 - Interface L2 end

        // Rfc 1533 7 - TCP begin
        TCPDefaultTTL,                          // 1 byte
        TCPKeepaliveInterval,                   // 1 uint (secs)
        TCPKeepaliveGarbage,                    // 1 byte = bool
        // Rfc 1533 7 - TCP end

        // Rfc 1533 8 - App/Svc begin
        NetworkInformationServiceDomain,        // variable ascii
        NetworkInformationServers,              // 1..n 4 byte
        NetworkTimeProtocolServers,             // 1..n 4 byte
        VendorSpecificInformation,              // 1..n 1 byte
        NetBIOSoverTCPIPNameServer,             // 1..n 4 byte
        NetBIOSoverTCPIPDistributionServer,     // 1..n 4 byte
        NetBIOSoverTCPIPNodeType,               // 1 byte
        NetBIOSoverTCPIPScope,                  // 1..n byte
        XWindowSystemFontServer,                // 1..n 4 byte
        XWindowSystemDisplayManager,            // 1..n 4 byte
        // Rfc 1533 8 - App/Svc end

        // Rfc 1533 9 - Dhcp begin
        RequestedIPAddress,                     // 4 byte
        IPAddressLeaseTime,                     // 1 uint (secs)
        OptionOverload,  // sname and/or fname contain options (1:file, 2:sname, 3:both)
        DhcpMessageType,                        // 1 byte
        ServerIdentifier,  // Us                // 4 byte
        ParameterRequestList,                   // 1..n option byte
        Message,                                // variable ascii   (Error message in nak)
        MaximumDHCPMessageSize,                 // ushort
        RenewalTimeValue_T1,                    // 1 uint (secs)
        RebindingTimeValue_T2,                  // 1 uint (secs)
        Vendorclassidentifier,                  // 1..n byte
        ClientIdentifier,                       // 2..n byte
        // Rfc 1533 9 - Dhcp end

        // Extension begin
        NetWateIPDomainName,
        NetWateIPInformation,
        NetworkInformationServicePlusDomain,
        NetworkInformationServicePlusServers,
        TFTPServerName,
        BootfileName,
        MobileIPHomeAgent,
        SMTPServer,
        POP3Server,
        NNTPServer,
        DefaultWWWServer,
        DefaultFingerServer,
        DefaultIRCServer,
        StreetTalkServer,
        STDAServer,

        // 77 - 81
        RelayInfo = 82,
        // 83 - 120
        StaticRoutes = 121,
        // 122 - 248
        StaticRoutesWin = 249,
        // 250 - 254
    }
}
