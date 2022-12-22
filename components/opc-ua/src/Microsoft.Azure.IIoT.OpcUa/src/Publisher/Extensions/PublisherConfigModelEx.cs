// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    /// <summary>
    /// Publisher config model extensions
    /// </summary>
    public static class PublisherConfigModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EngineConfigurationModel Clone(this EngineConfigurationModel model) {
            if (model == null) {
                return null;
            }
            return new EngineConfigurationModel {
                BatchSize = model.BatchSize,
                BatchTriggerInterval = model.BatchTriggerInterval,
                DiagnosticsInterval = model.DiagnosticsInterval,
                MaxMessageSize = model.MaxMessageSize,
                MaxOutgressMessages = model.MaxOutgressMessages,
                UseStandardsCompliantEncoding = model.UseStandardsCompliantEncoding,
                EnableRoutingInfo = model.EnableRoutingInfo,
            };
        }
    }
}
