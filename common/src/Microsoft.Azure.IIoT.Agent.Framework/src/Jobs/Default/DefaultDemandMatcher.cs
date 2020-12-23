// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Default demand matcher
    /// </summary>
    public class DefaultDemandMatcher : IDemandMatcher {

        /// <summary>
        /// Match
        /// </summary>
        /// <param name="demands"></param>
        /// <param name="capabilities"></param>
        /// <returns></returns>
        public bool MatchCapabilitiesAndDemands(IEnumerable<DemandModel> demands,
            IDictionary<string, string> capabilities) {
            if (demands == null || !demands.Any()) {
                return true;
            }

            if (demands.Any() && (capabilities == null || !capabilities.Any())) {
                return false;
            }

            var success = true;
            foreach (var demand in demands) {
                switch (demand.Operator ?? DemandOperators.Equals) {
                    case DemandOperators.Exists:
                        if (!capabilities.Any(c => c.Key.Equals(demand.Key, StringComparison.OrdinalIgnoreCase))) {
                            success = false;
                        }
                        break;
                    case DemandOperators.Equals:
                        if (!capabilities.Any(c =>
                            c.Key.Equals(demand.Key, StringComparison.OrdinalIgnoreCase) &&
                            (c.Value != null) && c.Value.Equals(demand.Value))) {
                            success = false;
                        }
                        break;
                    case DemandOperators.Match:
                        if (!capabilities.Any(c =>
                            c.Key.Equals(demand.Key, StringComparison.OrdinalIgnoreCase) &&
                            Regex.IsMatch(c.Value, demand.Value))) {
                            success = false;
                        }
                        break;
                    default:
                        throw new NotImplementedException(
                            $"Operator '{demand.Operator} has not been implemented yet.'");
                }
                if (!success) {
                    break;
                }
            }

            return success;
        }
    }
}