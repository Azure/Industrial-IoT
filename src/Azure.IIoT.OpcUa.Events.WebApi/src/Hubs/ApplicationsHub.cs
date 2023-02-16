// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Events.WebApi {
    using Azure.IIoT.OpcUa.Events.WebApi.Auth;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;

    /// <summary>
    /// Applications hub
    /// </summary>
    [Route("v2/applications/events")]
    [Authorize(Policy = Policies.CanRead)]
    public class ApplicationsHub : Hub {

    }
}