// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Microsoft.Extensions.Options;
using System.Collections.Generic;

/// <summary>
/// Subscriber options
/// </summary>
public record class SubscribeOptions
{
    /// <summary>
    /// The monitored items to subscribe to
    /// </summary>
    public Dictionary<string, MonitoredItemOptions> MonitoredItems { get; init; } = [];

    /// <summary>
    /// Options
    /// </summary>
    public SubscriptionOptions? Options { get; init; }
}
