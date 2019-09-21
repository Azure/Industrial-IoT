// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Handles events
    /// </summary>
    public interface IEventSourceSubscriber {

        /// <summary>
        /// Level
        /// </summary>
        EventLevel Level { get; }

        /// <summary>
        /// New event
        /// </summary>
        /// <param name="eventData"></param>
        void OnEvent(EventWrittenEventArgs eventData);
    }
}

