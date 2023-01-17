// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Event filter extensions
    /// </summary>
    public static class EventFilterModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EventFilterModel Clone(this EventFilterModel model) {
            if (model == null) {
                return null;
            }
            return new EventFilterModel {
                SelectClauses = model.SelectClauses?
                    .Select(a => a.Clone())
                    .ToList(),
                WhereClause = model.WhereClause.Clone(),
                TypeDefinitionId = model.TypeDefinitionId
            };
        }

        /// <summary>
        /// Compare filters
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EventFilterModel model, EventFilterModel other) {
            if (model == null && other == null) {
                return true;
            }
            if (model == null || other == null) {
                return false;
            }
            if (!model.SelectClauses.SetEqualsSafe(other.SelectClauses,
                (x, y) => x.IsSameAs(y))) {
                return false;
            }
            if (!model.WhereClause.IsSameAs(other.WhereClause)) {
                return false;
            }
            if (model.TypeDefinitionId != other.TypeDefinitionId) {
                return false;
            }
            return true;
        }
    }
}