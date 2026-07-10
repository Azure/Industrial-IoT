// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Schema registry
    /// </summary>
    public interface ISchemaRegistry
    {
        /// <summary>
        /// Register a schema with the registry
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<string> RegisterAsync(IEventSchema schema,
            CancellationToken ct = default);
    }
}
