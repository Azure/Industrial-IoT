// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Exceptions {
    using Microsoft.Azure.IIoT.Exceptions;
    using System;

    /// <summary>
    /// Http request exception due to connectivity issues.
    /// </summary>
    public class HttpRequestException : CommunicationException, ITransientException {

        /// <inheritdocs/>
        public HttpRequestException(string message) : 
            base(message) {
        }

        /// <inheritdocs/>
        public HttpRequestException(string message, 
            Exception innerException) : base(message, innerException) {
        }
    }
}
