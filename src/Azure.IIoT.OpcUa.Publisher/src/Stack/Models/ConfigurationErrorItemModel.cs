// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Error item
    /// </summary>
    public record class ConfigurationErrorItemModel : BaseItemModel
    {
        /// <summary>
        /// Error state of item
        /// </summary>
        public required ServiceResultModel State { get; init; }

        /// <summary>
        /// Node id that was mis-configured
        /// </summary>
        public required string NodeId { get; init; }
    }
}
