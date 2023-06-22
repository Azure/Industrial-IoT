// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Credential model extensions
    /// </summary>
    public static class CredentialModelEx
    {
        /// <summary>
        /// Get password
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string? GetPassword(this CredentialModel? model)
        {
            if (model?.Type == CredentialType.UserName &&
                model.Value?.IsObject == true &&
                model.Value.TryGetProperty("password", out var password) &&
                password.IsString)
            {
                return (string?)password;
            }
            return null;
        }

        /// <summary>
        /// Get user name
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string? GetUserName(this CredentialModel? model)
        {
            if (model?.Type == CredentialType.UserName &&
                model.Value?.IsObject == true &&
                model.Value.TryGetProperty("user", out var user) &&
                user.IsString)
            {
                return (string?)user;
            }
            return null;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static CredentialModel? Clone(this CredentialModel? model)
        {
            return model == null ? null : (model with
            {
                Value = model.Value?.Copy()
            });
        }
    }
}
