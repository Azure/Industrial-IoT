// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Exceptions {
    using System;

    /// <summary>
    /// Session not empty
    /// </summary>
    public class SessionNotEmptyException : Exception {

        /// <inheritdoc/>
        public SessionNotEmptyException(string sessionId) :
            base($"The session with Id '{sessionId}' is not empty and still contains subscriptions.") { }
    }
}