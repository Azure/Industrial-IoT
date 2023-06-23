// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Furly.Extensions.Serializers;

    /// <summary>
    /// Extension field template
    /// </summary>
    public sealed record class ExtensionFieldModel : BaseMonitoredItemModel
    {
        /// <summary>
        /// Value of the extension field to inject
        /// </summary>
        public required VariantValue Value { get; set; }
    }
}
