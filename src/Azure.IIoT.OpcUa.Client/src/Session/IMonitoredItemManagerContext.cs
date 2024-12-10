// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Microsoft.Extensions.Options;

/// <summary>
/// Context for monitored item manager
/// </summary>
public interface IMonitoredItemManagerContext
{
    /// <summary>
    /// Monitored item services
    /// </summary>
    IMonitoredItemServiceSet Session { get; }

    /// <summary>
    /// Method call services
    /// </summary>
    IMethodServiceSet Methods { get; }

    /// <summary>
    /// Subscription id
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Create monitored item
    /// </summary>
    /// <param name="name"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    MonitoredItem CreateMonitoredItem(string name,
        IOptionsMonitor<MonitoredItemOptions> options);

    /// <summary>
    /// Update
    /// </summary>
    void Update();
}
