// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Sessions;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Method services
/// </summary>
public interface IMethodServiceSet
{
    /// <summary>
    /// Call service
    /// </summary>
    /// <param name="requestHeader"></param>
    /// <param name="methodsToCall"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<CallResponse> CallAsync(RequestHeader? requestHeader,
        CallMethodRequestCollection methodsToCall,
        CancellationToken ct = default);
}
