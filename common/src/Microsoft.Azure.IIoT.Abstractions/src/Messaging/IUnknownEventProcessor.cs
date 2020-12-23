// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles all events not yet handled
    /// </summary>
    public interface IUnknownEventProcessor : IHandler {

        /// <summary>
        /// Handle event
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        Task HandleAsync(byte[] eventData,
            IDictionary<string, string> properties);
    }
}
