// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {

    /// <summary>
    /// Authentication model extensions
    /// </summary>
    public static class AuthenticationModelEx {

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this AuthenticationModel model, AuthenticationModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return
                (that.TokenType ?? TokenType.None) ==
                    (model.TokenType ?? TokenType.None) &&
                that.User == model.User;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AuthenticationModel Clone(this AuthenticationModel model) {
            if (model == null) {
                return null;
            }
            return new AuthenticationModel {
                Token = model.Token?.DeepClone(),
                TokenType = model.TokenType,
                User = model.User
            };
        }
    }
}
