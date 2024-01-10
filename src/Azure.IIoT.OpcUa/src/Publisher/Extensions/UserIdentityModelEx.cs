// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// User Identity model extensions
    /// </summary>
    public static class UserIdentityModelEx
    {
        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this UserIdentityModel? model, UserIdentityModel? that)
        {
            if (model == that)
            {
                return true;
            }

            model ??= new UserIdentityModel();
            that ??= new UserIdentityModel();

            if ((that.User ?? string.Empty) != (model.User ?? string.Empty))
            {
                return false;
            }
            if ((that.Password ?? string.Empty) != (model.Password ?? string.Empty))
            {
                return false;
            }
            if ((that.Thumbprint ?? string.Empty) != (model.Thumbprint ?? string.Empty))
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
        public static UserIdentityModel? Clone(this UserIdentityModel? model)
        {
            return model == null ? null : (model with { });
        }
    }
}
