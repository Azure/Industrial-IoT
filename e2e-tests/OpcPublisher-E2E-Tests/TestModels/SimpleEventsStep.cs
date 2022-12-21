// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    /// <summary>Simple event step.</summary>
    public class SimpleEventsStep {
        /// <summary>Gets or sets the step name.</summary>
        /// <example>"Step 1"</example>
        public string Name { get; set; }

        /// <summary>Gets or sets the step duration in milliseconds.</summary>
        /// <example>1000.0</example>
        public double Duration { get; set; }
    }
}