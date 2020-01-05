// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Linq;

    /// <summary>
    /// Writer group Model extensions
    /// </summary>
    public static class WriterGroupModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WriterGroupModel Clone(this WriterGroupModel model) {
            if (model?.DataSetWriters == null) {
                return null;
            }
            return new WriterGroupModel {
                WriterGroupId = model.WriterGroupId,
                DataSetWriters = model.DataSetWriters
                    .Select(f => f.Clone())
                    .ToList(),
                HeaderLayoutUri = model.HeaderLayoutUri,
                KeepAliveTime = model.KeepAliveTime,
                LocaleIds = model.LocaleIds?.ToList(),
                MaxNetworkMessageSize = model.MaxNetworkMessageSize,
                MessageSettings = model.MessageSettings.Clone(),
                MessageType = model.MessageType,
                Name = model.Name,
                Priority = model.Priority,
                PublishingInterval = model.PublishingInterval,
                SecurityGroupId = model.SecurityGroupId,
                SecurityKeyServices = model.SecurityKeyServices?
                    .Select(c => c.Clone())
                    .ToList(),
                SecurityMode = model.SecurityMode,
            };
        }
    }
}