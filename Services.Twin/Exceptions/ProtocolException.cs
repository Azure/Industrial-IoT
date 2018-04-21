// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Exceptions {
    using Microsoft.Azure.IIoT.Common.Exceptions;
    using System;

    /// <summary>
    /// Thrown when failing to connect to resource
    /// </summary>
    public class ProtocolException : CommunicationException {

        public ProtocolException(string message) :
            base(message) {
        }

        public ProtocolException(string message, Exception innerException) :
            base(message, innerException) {
        }
    }
}