// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using System;

/// <summary>
/// Subscription context
/// </summary>
internal interface ISubscriptionContext : ISubscriptionServiceSet, IMethodServiceSet
{
    /// <summary>
    /// Current session timeout
    /// </summary>
    TimeSpan SessionTimeout { get; }
}
