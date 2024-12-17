// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Microsoft.Extensions.Options;

/// <summary>
/// Context for monitored item manager. The monitored item
/// manager manages the state of the monitored items in the
/// subscription.
/// </summary>
internal interface IMonitoredItemManagerContext
{
    /// <summary>
    /// Subscription id the monitored items are managed for
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Monitored item services
    /// </summary>
    IMonitoredItemServiceSet Session { get; }

    /// <summary>
    /// Method call services
    /// </summary>
    IMethodServiceSet Methods { get; }

    /// <summary>
    /// Create monitored item
    /// </summary>
    /// <param name="name"></param>
    /// <param name="options"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    MonitoredItem CreateMonitoredItem(string name,
        IOptionsMonitor<MonitoredItemOptions> options,
        IMonitoredItemContext context);

    /// <summary>
    /// Update
    /// </summary>
    void Update();
}
