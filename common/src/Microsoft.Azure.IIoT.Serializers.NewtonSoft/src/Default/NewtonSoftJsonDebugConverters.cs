// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers.NewtonSoft {
    /// <summary>
    /// Debug converters
    /// </summary>
    public class NewtonSoftJsonDebugConverters : NewtonSoftJsonConverters {

        /// <summary>
        /// Create provider
        /// </summary>
        public NewtonSoftJsonDebugConverters() : base(true) {
        }
    }
}
