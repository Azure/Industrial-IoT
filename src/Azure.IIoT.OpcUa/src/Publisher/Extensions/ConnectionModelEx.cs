// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Connection endpoint model extensions
    /// </summary>
    public static class ConnectionModelEx
    {
        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this ConnectionModel? model, ConnectionModel? that)
        {
            if (model == that)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            if (that.Group != model.Group)
            {
                return false;
            }
            if (that.IsReverse != model.IsReverse)
            {
                return false;
            }
            if (!that.Endpoint.IsSameAs(model.Endpoint))
            {
                return false;
            }
            if (that.Diagnostics?.AuditId != model.Diagnostics?.AuditId)
            {
                return false;
            }
            if (!that.User.IsSameAs(model.User))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Is this reverse connected
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static bool IsReverseConnect(this ConnectionModel connection)
        {
            return connection.IsReverse == true && connection.GetEndpointUrls().Any();
        }

        /// <summary>
        /// Get endpont urls to try from connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static IEnumerable<Uri> GetEndpointUrls(this ConnectionModel connection)
        {
            if (connection.Endpoint?.Url == null)
            {
                return Enumerable.Empty<Uri>();
            }
            var endpoints = new Uri(connection.Endpoint.Url).YieldReturn();
            if (connection.Endpoint.AlternativeUrls != null)
            {
                endpoints = endpoints.Concat(connection.Endpoint.AlternativeUrls
                    .Where(u => !string.IsNullOrEmpty(u))
                    .Select(u => new Uri(u)));
            }
            return endpoints.Where(u => connection.IsReverse != true ||
                string.Equals(u.Scheme, "opc.tcp", // Only allow tcp scheme
                    StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Throw if invalid
        /// </summary>
        /// <param name="model"></param>
        /// <param name="paramName"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ThrowIfInvalid(this ConnectionModel? model, string paramName)
        {
            ArgumentNullException.ThrowIfNull(model, paramName);
            // TODO: add more
            if (!model.GetEndpointUrls().Any())
            {
                throw new ArgumentException("Missing endpoints in connection",
                    paramName);
            }
        }

        /// <summary>
        /// Create unique hash
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int CreateConsistentHash(this ConnectionModel model)
        {
            var hashCode = -1971667340;
            hashCode = (hashCode * -1521134295) +
                model.Endpoint?.CreateConsistentHash() ?? 0;
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<CredentialModel>.Default.GetHashCode(model.User ?? new CredentialModel());
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(model.Diagnostics?.AuditId ?? string.Empty);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(model.Group ?? string.Empty);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<bool>.Default.GetHashCode(model.IsReverse ?? false);
            return hashCode;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static ConnectionModel? Clone(this ConnectionModel? model)
        {
            return model == null ? null : (model with
            {
                Endpoint = model.Endpoint.Clone(),
                User = model.User.Clone(),
                Diagnostics = model.Diagnostics.Clone()
            });
        }

        /// <summary>
        /// Returns a string that uniquiely identifies the connection based on
        /// endpoint url, hash and associated group
        /// </summary>
        /// <param name="model"></param>
        public static string? CreateConnectionId(this ConnectionModel? model)
        {
            if (string.IsNullOrEmpty(model?.Endpoint?.Url))
            {
                return null;
            }
            return !string.IsNullOrEmpty(model.Group) ?
                $"{model.Endpoint?.Url}_{CreateConsistentHash(model):X8}_{model.Group}" :
                $"{model.Endpoint?.Url}_{CreateConsistentHash(model):X8}";
        }
    }
}
