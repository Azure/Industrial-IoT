// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua.Client;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Internal unsafe session access
    /// </summary>
    internal interface ISessionAccessor
    {
        /// <summary>
        /// Get an unsafe reference of the underlying session or
        /// null when no session was found.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        bool TryGetSession([NotNullWhen(true)] out ISession? session);
    }
}
