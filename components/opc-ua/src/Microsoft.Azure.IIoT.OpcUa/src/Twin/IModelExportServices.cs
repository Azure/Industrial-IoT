// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin upload services
    /// </summary>
    public interface IUploadServices<T> {

        /// <summary>
        /// Start exporting model from endpoint to blob
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <returns>file name of the model exported</returns>
        Task<ModelUploadStartResultModel> ModelUploadStartAsync(T endpoint,
            ModelUploadStartRequestModel request);
    }
}
