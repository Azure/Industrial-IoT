// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;

    /// <summary>
    /// Connection endpoint model extensions
    /// </summary>
    public static class ConnectionModelEx {

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this ConnectionModel model, ConnectionModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            if (!that.Endpoint.IsSameAs(model.Endpoint)) {
                return false;
            }
            if (that.Diagnostics?.AuditId != model.Diagnostics?.AuditId) {
                return false;
            }
            if (!VariantValue.DeepEquals(that.User?.Value, model.User?.Value)) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create unique hash
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static int CreateConsistentHash(this ConnectionModel model) {
            var hashCode = -1971667340;
            hashCode = (hashCode * -1521134295) +
                model.Endpoint.CreateConsistentHash();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<VariantValue>.Default.GetHashCode(model.User?.Value);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(model.Diagnostics?.AuditId);
            return hashCode;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ConnectionModel Clone(this ConnectionModel model) {
            if (model == null) {
                return null;
            }
            return new ConnectionModel {
                Endpoint = model.Endpoint.Clone(),
                User = model.User.Clone(),
                Diagnostics = model.Diagnostics.Clone()
            };
        }
    }
}