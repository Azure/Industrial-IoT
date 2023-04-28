// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa
{
    using System.Diagnostics;

    /// <summary>
    /// Metrics context
    /// </summary>
    public interface IMetricsContext
    {
        /// <summary>
        /// Tag list
        /// </summary>
        TagList TagList { get; }

        /// <summary>
        /// Null metrics context
        /// </summary>
        public static IMetricsContext Empty { get; }
            = new EmptyContext();

        /// <summary>
        /// Empty context
        /// </summary>
        private sealed class EmptyContext : IMetricsContext
        {
            /// <inheritdoc/>
            public TagList TagList { get; }
        }
    }
}
