// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using Furly.Extensions.Serializers;

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
            if (that.Value.IsNull() || model.Value.IsNull()) {
                if (that.Value.IsNull() && model.Value.IsNull()) {
                    return true;
                }
                return false;
            }
            if (!VariantValue.DeepEquals(that.Value, model.Value)) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get password
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GetPassword(this CredentialModel model) {
            if (model?.Type == CredentialType.UserName &&
                model.Value?.IsObject == true &&
                model.Value.TryGetProperty("password", out var password) &&
                password.IsString) {
                return (string)password;
            }
            return null;
        }

        /// <summary>
        /// Get user name
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GetUserName(this CredentialModel model) {
            if (model?.Type == CredentialType.UserName &&
                model.Value?.IsObject == true &&
                model.Value.TryGetProperty("user", out var user) &&
                user.IsString) {
                return (string)user;
            }
            return null;
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
                Value = model.Value?.Copy(),
                Type = model.Type
            };
        }
    }
}
