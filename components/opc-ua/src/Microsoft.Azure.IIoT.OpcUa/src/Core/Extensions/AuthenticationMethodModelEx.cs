// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Authentication method model extensions
    /// </summary>
    public static class AuthenticationMethodModelEx {

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IEnumerable<AuthenticationMethodModel> model,
            IEnumerable<AuthenticationMethodModel> that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            if (model.Count() != that.Count()) {
                return false;
            }
            return model.All(a => that.Any(b => b.IsSameAs(a)));
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this AuthenticationMethodModel model,
            AuthenticationMethodModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            if (model.Configuration != null && that.Configuration != null) {
                if (!VariantValue.DeepEquals(model.Configuration, that.Configuration)) {
                    return false;
                }
            }
            return
                model.Id == that.Id &&
                model.SecurityPolicy == that.SecurityPolicy &&
                (that.CredentialType ?? CredentialType.None) ==
                    (model.CredentialType ?? CredentialType.None);
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AuthenticationMethodModel Clone(this AuthenticationMethodModel model) {
            if (model == null) {
                return null;
            }
            return new AuthenticationMethodModel {
                Configuration = model.Configuration?.Copy(),
                Id = model.Id,
                SecurityPolicy = model.SecurityPolicy,
                CredentialType = model.CredentialType
            };
        }
    }
}
