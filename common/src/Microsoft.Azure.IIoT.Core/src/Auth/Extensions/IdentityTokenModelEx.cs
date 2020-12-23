// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Models {
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Text;

    /// <summary>
    /// Identity token
    /// </summary>
    public static class IdentityTokenModelEx {

        /// <summary>
        /// Creates the authorization value from the token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string ToAuthorizationValue(this IdentityTokenModel model) {
            return Try.Op(() => Encoding.UTF8.GetBytes(
                ConnectionString.CreateFromAccessToken(model).ToString())
                    .ToBase64String());
        }

        /// <summary>
        /// Recreates the token from the authorization header value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IdentityTokenModel ToIdentityToken(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return null;
            }
            return Try.Op(() => ConnectionString.Parse(
                Encoding.UTF8.GetString(value.DecodeAsBase64()))
                    .ToIdentityToken());
        }
    }
}