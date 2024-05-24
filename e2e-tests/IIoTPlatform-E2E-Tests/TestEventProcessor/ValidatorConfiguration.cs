// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic
{
    /// <summary>
    /// Model class to encapsule the configuration required to monitor and validate IoT Hub events.
    /// </summary>
    public class ValidatorConfiguration
    {
        /// <summary>
        /// Gets or sets the expected number of value changes per timestamp
        /// </summary>
        public uint ExpectedValueChangesPerTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the expected time difference between values changes in milliseconds
        /// </summary>
        public uint ExpectedIntervalOfValueChanges { get; set; }

        /// <summary>
        /// Gets or sets the time difference between OPC UA Server fires event until Changes Received in IoT Hub in milliseconds
        /// </summary>
        public uint ExpectedMaximalDuration { get; set; }

        /// <summary>
        /// Gets or sets the value that will be used to define range within timings expected as equal (in milliseconds)
        /// Current Value need to be within range of Expected Value +/- threshold
        /// </summary>
        public uint ThresholdValue { get; set; } = 100;
    }
}
