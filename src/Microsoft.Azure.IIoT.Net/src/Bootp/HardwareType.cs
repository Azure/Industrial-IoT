// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Bootp {

    /// <summary>
    /// Reported hardware type
    /// </summary>
    public enum HardwareType : byte {
        None,
        Ethernet,
        ExperimentalEthernet,
        AmateurRadio,
        ProteonTokenRing,
        Chaos,
        IEEE802Networks,
        ArcNet,
        Hyperchnnel,
        Lanstar
    }
}
