// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Blob upload service
    /// </summary>
    public interface IBlobUpload : IIdentity {

        /// <summary>
        /// Upload blob
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        Task SendFileAsync(string fileName, Stream stream,
            string contentType);
    }
}
