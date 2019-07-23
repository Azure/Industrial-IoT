// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// This exception is thrown when a service was throttled and needs to be
    /// retried.
    /// </summary>
    public class TemporarilyBusyException : Exception, ITransientException {

        /// <summary>
        /// When to retry the call
        /// </summary>
        public TimeSpan? RetryAfter { get; }

        /// <inheritdoc />
        public TemporarilyBusyException(TimeSpan? retryAfter = null) {
            RetryAfter = retryAfter;
        }

        /// <inheritdoc />
        public TemporarilyBusyException(string message,
            TimeSpan? retryAfter = null) :
            base(message) {
            RetryAfter = retryAfter;
        }

        /// <inheritdoc />
        public TemporarilyBusyException(string message, Exception innerException,
            TimeSpan? retryAfter = null) :
            base(message, innerException) {
            RetryAfter = retryAfter;
        }
    }
}
