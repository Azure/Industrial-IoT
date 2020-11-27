// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic
{
    using System.Collections.ObjectModel;

    /// <summary>
    /// Represents the result of the Stop-Command of the TelemetryValidator.
    /// </summary>
    public class StopResult : IResult
    {
        /// <summary>
        /// Flag whether the monitoring was successful (without errors) or not.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The number of value changes by Node Id.
        /// </summary>
        public ReadOnlyDictionary<string, int> ValueChangesByNodeId { get; set; }
    }
}