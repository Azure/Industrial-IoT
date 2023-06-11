// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Event filter extensions
    /// </summary>
    public static class EventFilterModelEx
    {
        /// <summary>
        /// Compare filters
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EventFilterModel model, EventFilterModel other)
        {
            if (!model.SelectClauses.SetEqualsSafe(other.SelectClauses,
                (x, y) => x.IsSameAs(y)))
            {
                return false;
            }
            if (!model.WhereClause.IsSameAs(other.WhereClause))
            {
                return false;
            }
            if (model.TypeDefinitionId != other.TypeDefinitionId)
            {
                return false;
            }
            return true;
        }
    }
}
