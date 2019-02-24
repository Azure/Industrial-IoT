// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Export {
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Stubbed out upload functionality
    /// </summary>
    public sealed class UploadServicesStub<T> : IUploadServices<T> {

        /// <inheritdoc/>
        public Task<ModelUploadStartResultModel> ModelUploadStartAsync(
            T endpoint, ModelUploadStartRequestModel request) {
            return Task.FromResult(new ModelUploadStartResultModel());
        }
    }
}
