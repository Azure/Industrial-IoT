// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Parser
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Filter parser provides ability to convert filter query strings
    /// into content filters for query and subscription
    /// </summary>
    public interface IFilterParser
    {
        /// <summary>
        /// Parse query string into event filter model
        /// </summary>
        /// <param name="query"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EventFilterModel> ParseEventFilterAsync(string query,
            IFilterParserContext context, CancellationToken ct = default);
    }
}
