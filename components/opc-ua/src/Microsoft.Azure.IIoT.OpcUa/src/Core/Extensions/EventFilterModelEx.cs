// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
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
                WhereClause = model.WhereClause.Clone()
            };
        }
    }
}