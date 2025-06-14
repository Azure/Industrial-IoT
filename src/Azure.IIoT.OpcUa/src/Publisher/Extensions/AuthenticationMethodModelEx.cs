// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Authentication method model extensions
    /// </summary>
    public static class AuthenticationMethodModelEx
    {
        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IReadOnlyList<AuthenticationMethodModel>? model,
            IReadOnlyList<AuthenticationMethodModel>? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (model.Count != that.Count)
            {
                return false;
            }
            foreach (var a in model)
            {
                if (!that.Any(b => b.IsSameAs(a)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this AuthenticationMethodModel? model,
            AuthenticationMethodModel? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (model.Configuration != null && that.Configuration != null &&
                !VariantValue.DeepEquals(model.Configuration, that.Configuration))
            {
                return false;
            }
            return
                model.Id == that.Id &&
                model.SecurityPolicy == that.SecurityPolicy &&
                model.CredentialType == that.CredentialType;
        }
    }
}
