// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models {
    using System;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;

    public class PublisherInfo {

        /// <summary>
        /// Publisher model
        /// </summary>
        public PublisherApiModel PublisherModel { get; set; }

        public bool TryUpdateData(PublisherInfoRequested input) {
            try {
                PublisherModel.Configuration ??= new PublisherConfigApiModel();

                if (!string.IsNullOrEmpty(input.RequestedMaxWorkers)) {
                    PublisherModel.Configuration.MaxWorkers = int.Parse(input.RequestedMaxWorkers);
                }
                else {
                    PublisherModel.Configuration.MaxWorkers = -1;
                }

                if (!string.IsNullOrEmpty(input.RequestedHeartbeatInterval)) {
                    PublisherModel.Configuration.HeartbeatInterval = TimeSpan.FromSeconds(Convert.ToDouble(input.RequestedHeartbeatInterval));
                }
                else {
                    PublisherModel.Configuration.HeartbeatInterval = TimeSpan.MinValue;
                }

                if (!string.IsNullOrEmpty(input.RequestedJobCheckInterval)) {
                    PublisherModel.Configuration.JobCheckInterval = TimeSpan.FromSeconds(Convert.ToDouble(input.RequestedJobCheckInterval));
                }
                else {
                    PublisherModel.Configuration.JobCheckInterval = TimeSpan.MinValue;
                }

                return true;
            }
            catch (Exception) {
                return false;
            }
        }
    }
}
