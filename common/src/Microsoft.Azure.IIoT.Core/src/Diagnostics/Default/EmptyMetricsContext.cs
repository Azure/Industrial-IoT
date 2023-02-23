// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System.Diagnostics;

    /// <summary>
    /// Empty context
    /// </summary>
    public sealed class EmptyMetricsContext : IMetricsContext {
        /// <inheritdoc/>
        public TagList TagList { get; }
    }
}
