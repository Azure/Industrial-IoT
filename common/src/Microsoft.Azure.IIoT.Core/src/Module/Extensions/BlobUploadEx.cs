// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Blob upload extensions
    /// </summary>
    public static class BlobUploadEx {

        /// <summary>
        /// Send from file system file
        /// </summary>
        /// <param name="upload"></param>
        /// <param name="fileName"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static async Task SendFileAsync(this IBlobUpload upload,
            string fileName, string contentType) {
            using (var file = new FileStream(fileName, FileMode.Open)) {
                await upload.SendFileAsync(fileName, file, contentType);
            }
        }
    }
}
