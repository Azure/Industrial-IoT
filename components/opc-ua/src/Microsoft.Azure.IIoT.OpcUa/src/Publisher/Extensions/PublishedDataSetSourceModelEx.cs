// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Dataset source extensions
    /// </summary>
    public static class PublishedDataSetSourceModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublishedDataSetSourceModel Clone(this PublishedDataSetSourceModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataSetSourceModel {
                Connection = model.Connection.Clone(),
                PublishedEvents = model.PublishedEvents.Clone(),
                PublishedVariables = model.PublishedVariables.Clone(),
                SubscriptionSettings = model.SubscriptionSettings.Clone()
            };
        }

        /// <summary>
        /// Create hash
        /// </summary>
        /// <returns></returns>
        public static string GetHashSafe(this PublishedDataSetSourceModel model) {
            var publishedVariableData = model.PublishedVariables.PublishedData.FirstOrDefault();
            var publishedEventData = model.PublishedEvents.PublishedData.FirstOrDefault();
            var sb = new StringBuilder();
            sb.Append(model.Connection?.Endpoint?.Url);
            sb.Append(model.Connection?.Endpoint?.SecurityMode.ToString());
            sb.Append(model.Connection?.Endpoint?.SecurityPolicy);
            sb.Append(model.Connection?.User?.Type.ToString());
            sb.Append(model.Connection?.User?.Value.ToJson());
            sb.Append(model.SubscriptionSettings?.PublishingInterval.ToString());
            sb.Append(publishedVariableData?.Id);
            sb.Append(publishedVariableData?.PublishedVariableNodeId);
            sb.Append(publishedVariableData?.DataSetClassFieldId);
            sb.Append(publishedVariableData?.PublishedVariableDisplayName);
            sb.Append(publishedVariableData?.SamplingInterval);
            sb.Append(publishedVariableData?.HeartbeatInterval);
            sb.Append(publishedVariableData?.SkipFirst);
            sb.Append(publishedEventData?.Id);
            sb.Append(publishedEventData?.EventNotifier);
            if (publishedEventData?.BrowsePath != null) {
                foreach (var browsePath in publishedEventData.BrowsePath) {
                    sb.Append(browsePath);
                }
            }
            sb.Append(publishedEventData?.ConditionHandling?.UpdateInterval);
            sb.Append(publishedEventData?.ConditionHandling?.SnapshotInterval);
            sb.Append(publishedEventData?.TypeDefinitionId);
            return sb.ToString().ToSha1Hash();
        }
    }
}
