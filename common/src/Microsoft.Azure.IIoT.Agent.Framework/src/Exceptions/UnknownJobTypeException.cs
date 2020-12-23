// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Exceptions {
    using System;

    /// <summary>
    /// Unknown job
    /// </summary>
    [Serializable]
    public class UnknownJobTypeException : Exception {

        /// <inheritdoc/>
        public UnknownJobTypeException(string jobType) :
            base($"Job type '{jobType}' it not known.") {
        }
    }
}