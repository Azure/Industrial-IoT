// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.SignalR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;

    /// <summary>
    /// Gateway hub
    /// </summary>
    [MapTo("events/v2/gateways/events")]
    [Authorize(Policy = Policies.CanRead)]
    public class GatewaysHub : Hub;
}
