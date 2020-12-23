// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides access to twin properties
    /// </summary>
    public interface ITwinProperties : IIdentity {

        /// <summary>
        /// Resynchronize the twin with the cloud
        /// </summary>
        /// <returns></returns>
        Task RefreshAsync();

        /// <summary>
        /// Get all properties the twin has reported and thus applied.
        /// </summary>
        IReadOnlyDictionary<string, VariantValue> Reported { get; }
    }
}
