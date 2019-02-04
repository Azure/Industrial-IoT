// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------



using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Types
{
    public enum ApplicationType : int
    {
        Server = 0,
        Client = 1,
        ClientAndServer = 2,
        DiscoveryServer = 3
    }

    public enum ApplicationState : int
    {
        New = 0,
        Approved = 1,
        Rejected = 2,
        Unregistered = 3,
        Deleted = 4
    }

    [Flags]
    public enum QueryApplicationState : uint
    {
        Any = 0,
        New = 1,
        Approved = 2,
        Rejected = 4,
        Unregistered = 8,
        Deleted = 16
    }

}

