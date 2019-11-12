// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Matches demands
    /// </summary>
    public interface IDemandMatcher {

        /// <summary>
        /// Match capabilities and demands
        /// </summary>
        /// <param name="demands"></param>
        /// <param name="capabilities"></param>
        /// <returns></returns>
        bool MatchCapabilitiesAndDemands(IEnumerable<DemandModel> demands,
            IDictionary<string, string> capabilities);
    }
}