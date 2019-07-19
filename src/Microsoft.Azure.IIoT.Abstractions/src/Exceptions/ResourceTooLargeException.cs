// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Exceptions {
    using System;

    /// <summary>
    /// Thrown when a resource does not fit into the allowed
    /// storage allocation
    /// </summary>
    public class ResourceTooLargeException : Exception {

        /// <summary>
        /// Actual size
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Max allowed size
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="size"></param>
        /// <param name="maxSize"></param>
        public ResourceTooLargeException(string message,
            int size, int maxSize) : base(message) {

            Size = size;
            MaxSize = maxSize;
        }

        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="message"></param>
        public ResourceTooLargeException(string message) :
            this(message, -1, -1) {
        }
    }
}
