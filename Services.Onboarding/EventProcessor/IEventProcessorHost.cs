// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Onboarding.EventHub {
    using System.Threading.Tasks;

    public interface IEventProcessorHost {

        /// <summary>
        /// Start host
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Stop host
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}