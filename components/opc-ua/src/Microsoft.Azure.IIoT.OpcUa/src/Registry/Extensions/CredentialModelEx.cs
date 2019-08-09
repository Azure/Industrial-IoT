// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Credential model extensions
    /// </summary>
    public static class CredentialModelEx {

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this CredentialModel model,
            CredentialModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            if ((that.Type ?? CredentialType.None) !=
                    (model.Type ?? CredentialType.None)) {
                return false;
            }
            if (that.Value == model.Value) {
                return true;
            }
            if (that.Value == null || model.Value == null) {
                return false;
            }
            if (!JToken.DeepEquals(that.Value, model.Value)) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static CredentialModel Clone(this CredentialModel model) {
            if (model == null) {
                return null;
            }
            return new CredentialModel {
                Value = model.Value?.DeepClone(),
                Type = model.Type
            };
        }
    }
}
