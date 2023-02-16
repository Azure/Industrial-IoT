// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Events.WebApi {
    using Azure.IIoT.OpcUa.Events.WebApi.Auth;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Azure.IIoT.Messaging.SignalR;

    /// <summary>
    /// Supervisors hub
    /// </summary>
    [Route("v2/supervisors/events")]
    [Authorize(Policy = Policies.CanRead)]
    public class SupervisorsHub : Hub {

    }
}