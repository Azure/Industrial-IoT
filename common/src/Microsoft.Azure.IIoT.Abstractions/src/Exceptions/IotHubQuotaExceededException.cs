// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// IotHub Quota Exceeded
    /// </summary>
    /// 
    public class IotHubQuotaExceededException : Exception {
        
        /// <inheritdoc />
        public IotHubQuotaExceededException(string message) :
            base(message) {
        }

        /// <inheritdoc />
        public IotHubQuotaExceededException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}
