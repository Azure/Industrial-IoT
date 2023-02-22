// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {
#if ZOMBIE

    /// <summary>
    /// Variant extensions
    /// </summary>
    public static class ValueEx {
#if ZOMBIE

        /// <summary>
        /// hashes variant object
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToSha1Hash(this VariantValue val) {
            return val.ToJson().ToSha1Hash();
        }
#endif
    }
#endif
}
