// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to validate incoming message on an IoT Hub.
    /// </summary>
    public interface ITelemetryValidator
    {
        /// <summary>
        /// Method that runs asynchronously to connect to event hub and check
        /// a) if all expected value changes are delivered
        /// b) that time between value changes is expected
        /// </summary>
        /// <param name="token">Token to cancel the operation</param>
        /// <returns>Task that run until token is canceled</returns>
        Task<StartResult> StartAsync(ValidatorConfiguration configuration);

        /// <summary>
        /// Stop the monitoring and disposes all related resources..
        /// </summary>
        /// <returns></returns>
        Task<StopResult> StopAsync();
    }
}