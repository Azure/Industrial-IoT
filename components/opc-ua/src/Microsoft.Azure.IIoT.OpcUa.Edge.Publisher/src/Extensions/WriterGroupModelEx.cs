﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime;

    /// <summary>
    /// Extensions for data set writer
    /// </summary>
    public static class WriterGroupModelEx {

        /// <summary>
        /// Convert to message trigger configuration
        /// </summary>
        /// <param name="model"></param>
        /// <param name="publisherId"></param>
        /// <returns></returns>
        public static IWriterGroupConfig ToWriterGroupJobConfiguration(
            this WriterGroupJobModel model, string publisherId) {
            return new WriterGroupJobConfig {
                BatchSize = model.Engine?.BatchSize,
                BatchTriggerInterval = model.Engine?.BatchTriggerInterval,
                PublisherId = publisherId,
                DiagnosticsInterval = model.Engine?.DiagnosticsInterval,
                WriterGroup = model.WriterGroup,
                MaxMessageSize = model.Engine?.MaxMessageSize
            };
        }
    }
}