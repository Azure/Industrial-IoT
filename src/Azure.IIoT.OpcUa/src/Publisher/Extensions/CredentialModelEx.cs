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
                model.Value != null)
            {
                return model.Value.Password;
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
                model.Value != null)
            {
                return model.Value.User;
            }
            return null;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this CredentialModel? model, CredentialModel? that)
        {
            if (model == that)
            {
                return true;
            }

            model ??= new CredentialModel();
            that ??= new CredentialModel();

            if (that.Type != model.Type)
            {
                return false;
            }
            if (!that.Value.IsSameAs(model.Value))
            {
                return false;
            }
            return true;
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
                Value = model.Value.Clone()
            });
        }
    }
}
