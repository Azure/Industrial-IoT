// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher sample processing
    /// </summary>
    public interface ISubscriberSampleProcessor {

        /// <summary>
        /// Handle individual Pub/Sub sample
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        Task OnSubscriberSampleAsync(SubscriberSampleModel sample);
    }
}
