// Copyright (c) Microsoft. All rights reserved.
// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions {
    using System;

    /// <summary>
    /// This exception is thrown when an external dependency returns any error
    /// </summary>
    public class MethodCallStatusException : Exception {

        internal MethodCallStatusException(int result, string response) {
            Result = result;
            Response = response;
        }

        public int Result { get; }

        public string Response { get; }
    }
}
