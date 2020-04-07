// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {

    /// <summary>
    /// Variant discriminator
    /// </summary>
    public enum VariantValueType {

        /// <summary>
        /// Null
        /// </summary>
        Null,

        /// <summary>
        /// Array
        /// </summary>
        Values,

        /// <summary>
        /// Object
        /// </summary>
        Object,

        /// <summary>
        /// String
        /// </summary>
        Primitive
    }
}