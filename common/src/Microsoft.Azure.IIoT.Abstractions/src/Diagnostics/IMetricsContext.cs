// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System.Diagnostics;

    /// <summary>
    /// Metrics context
    /// </summary>
    public interface IMetricsContext {
        /// <summary>
        /// Tag list
        /// </summary>
        TagList TagList { get; }
    }
}