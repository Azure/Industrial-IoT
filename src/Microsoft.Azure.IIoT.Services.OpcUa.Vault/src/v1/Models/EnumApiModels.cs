// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{

    public enum ApplicationType : int
    {
        [EnumMember(Value = "server")]
        Server = 0,
        [EnumMember(Value = "client")]
        Client = 1,
        [EnumMember(Value = "clientAndServer")]
        ClientAndServer = 2,
        [EnumMember(Value = "discoveryServer")]
        DiscoveryServer = 3
    }

    public enum ApplicationState : int
    {
        [EnumMember(Value = "new")]
        New = 0,
        [EnumMember(Value = "approved")]
        Approved = 1,
        [EnumMember(Value = "rejected")]
        Rejected = 2,
        [EnumMember(Value = "unregistered")]
        Unregistered = 3,
        [EnumMember(Value = "deleted")]
        Deleted = 4
    }

    public enum CertificateRequestState
    {
        [EnumMember(Value = "new")]
        New = 0,
        [EnumMember(Value = "approved")]
        Approved = 1,
        [EnumMember(Value = "rejected")]
        Rejected = 2,
        [EnumMember(Value = "accepted")]
        Accepted = 3,
        [EnumMember(Value = "deleted")]
        Deleted = 4,
        [EnumMember(Value = "revoked")]
        Revoked = 5,
        [EnumMember(Value = "removed")]
        Removed = 6
    }

    [Flags]
    public enum QueryApplicationType : uint
    {
        [EnumMember(Value = "any")]
        Any = 0,
        [EnumMember(Value = "server")]
        Server = 1,
        [EnumMember(Value = "client")]
        Client = 2,
        [EnumMember(Value = "clientAndServer")]
        ClientAndServer = 3
    }

    [Flags]
    public enum QueryApplicationState : uint
    {
        [EnumMember(Value = "any")]
        Any = 0,
        [EnumMember(Value = "new")]
        New = 1,
        [EnumMember(Value = "approved")]
        Approved = 2,
        [EnumMember(Value = "rejected")]
        Rejected = 4,
        [EnumMember(Value = "unregistered")]
        Unregistered = 8,
        [EnumMember(Value = "deleted")]
        Deleted = 16
    }

}
