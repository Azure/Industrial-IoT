// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Endpoint model extensions
    /// </summary>
    public static class EndpointModelEx
    {
        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EndpointModel? model, EndpointModel? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (!that.HasSameSecurityProperties(model))
            {
                return false;
            }
            if (!that.GetAllUrls().SequenceEqualsSafe(model.GetAllUrls()))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool HasSameSecurityProperties(this EndpointModel? model, EndpointModel? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (!that.Certificate.SequenceEqualsSafe(model.Certificate))
            {
                return false;
            }
            if (that.SecurityPolicy != model.SecurityPolicy &&
                that.SecurityPolicy != null && model.SecurityPolicy != null)
            {
                return false;
            }
            if ((that.SecurityMode ?? SecurityMode.NotNone) !=
                    (model.SecurityMode ?? SecurityMode.NotNone))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create unique hash
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static int CreateConsistentHash(this EndpointModel endpoint)
        {
            var hashCode = -1971667340;
            hashCode = (hashCode * -1521134295) +
                endpoint.GetAllUrls().SequenceGetHashSafe();
            hashCode = (hashCode * -1521134295) +
                endpoint.Certificate.SequenceGetHashSafe();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(
                    endpoint.SecurityPolicy ?? string.Empty);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<SecurityMode?>.Default.GetHashCode(
                    endpoint.SecurityMode ?? SecurityMode.NotNone);

            return hashCode;
        }

        /// <summary>
        /// Get all urls
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllUrls(this EndpointModel? model)
        {
            if (model != null)
            {
                if (model.Url != null)
                {
                    yield return model.Url;
                }
                if (model.AlternativeUrls != null)
                {
                    foreach (var url in model.AlternativeUrls)
                    {
                        yield return url;
                    }
                }
            }
        }

        /// <summary>
        /// Create Union with endpoint
        /// </summary>
        /// <param name="model"></param>
        /// <param name="endpoint"></param>
        public static void UnionWith(this EndpointModel model,
            EndpointModel? endpoint)
        {
            if (endpoint == null)
            {
                return;
            }

            var alternativeUrls = model.AlternativeUrls.MergeWith(
                endpoint.AlternativeUrls)?.ToHashSet() ?? [];
            if (model.Url != null)
            {
                if (endpoint.Url != null)
                {
                    alternativeUrls.Add(endpoint.Url);
                }
                alternativeUrls.Remove(model.Url);
            }
            else
            {
                model.Url = endpoint.Url;
            }
            model.AlternativeUrls = alternativeUrls;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static EndpointModel? Clone(this EndpointModel? model)
        {
            return model == null ? null : (model with
            {
                AlternativeUrls = model.AlternativeUrls.ToHashSetSafe()
            });
        }
    }
}
