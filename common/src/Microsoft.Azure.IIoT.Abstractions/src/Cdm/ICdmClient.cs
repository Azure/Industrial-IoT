// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Cdm {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Cdm.Models;

    /// <summary>
    /// Handler for the CDM storage
    /// </summary>
    public interface ICdmClient : IDisposable {

        /// <summary>
        /// Iniotialize the CDM client
        /// </summary>
        /// <returns></returns>
        Task OpenAsync();

        /// <summary>
        /// Process the payload 
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="properties"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        Task ProcessAsync(SubscriberCdmSampleModel payload, 
            IDictionary<string, string> properties = null, string partitionKey = null);

        /// <summary>
        /// Close the CDM Client
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}
