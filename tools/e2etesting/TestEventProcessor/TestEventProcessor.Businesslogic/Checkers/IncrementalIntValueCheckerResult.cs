// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers {

    /// <summary>
    /// Result of IncrementalIntValueChecker monitoring.
    /// </summary>
    class IncrementalIntValueCheckerResult {

        /// <summary>
        /// Indicates number of dropped messages that were observed. It is calculated by detecting
        /// gaps in the value changes.
        /// </summary>
        public uint DroppedValueCount { get; set; }

        /// <summary>
        /// Indicates number of duplicate messages that were observed.
        /// </summary>
        public uint DuplicateValueCount { get; set; }
    }
}