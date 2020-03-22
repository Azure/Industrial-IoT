// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Handler that handles blob file upload notifications
    /// </summary>
    public interface IDeviceFileUploadHandler : IHandler {

        /// <summary>
        /// Handle blob file upload notifications.
        /// </summary>
        /// <param name="deviceId"></param>s
        /// <param name="moduleId"></param>
        /// <param name="blobName"></param>
        /// <param name="contentType"></param>
        /// <param name="blobUri"></param>
        /// <param name="enqueuedTimeUtc"></param>
        /// <param name="ct"></param>
        /// <returns>Whether the blob was procssed</returns>
        Task HandleAsync(string deviceId, string moduleId,
            string blobName, string contentType, string blobUri,
            DateTime enqueuedTimeUtc, CancellationToken ct);
    }
}
