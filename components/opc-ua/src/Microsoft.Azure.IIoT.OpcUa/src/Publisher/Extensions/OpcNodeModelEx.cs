// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Dataset source extensions
    /// </summary>
    public static class OpcNodeModelEx {

        /// <summary>
        /// Get comparer class for OpcNodeModel objects.
        /// </summary>
        public static EqualityComparer<OpcNodeModel> Comparer { get; } =
            new OpcNodeModelComparer();

        /// <summary>
        /// Check if nodes are equal
        /// </summary>
        public static bool IsSame(this OpcNodeModel model, OpcNodeModel that, int? defaultPublishing = null) {

            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }

            if (string.Compare(model.Id, that.Id, StringComparison.OrdinalIgnoreCase) != 0) {
                return false;
            }
            if (string.Compare(model.DisplayName, that.DisplayName, StringComparison.OrdinalIgnoreCase) != 0) {
                return false;
            }
            if (string.Compare(model.DataSetFieldId, that.DataSetFieldId, StringComparison.OrdinalIgnoreCase) != 0) {
                return false;
            }
            if (string.Compare(model.ExpandedNodeId, that.ExpandedNodeId, StringComparison.OrdinalIgnoreCase) != 0) {
                return false;
            }
            if (defaultPublishing.HasValue) {
                if (model.OpcPublishingInterval.GetValueOrDefault(defaultPublishing.Value) !=
                    that.OpcPublishingInterval.GetValueOrDefault(defaultPublishing.Value)) {
                    return false;
                }
            }
            else {
                if (model.OpcPublishingInterval != that.OpcPublishingInterval) {
                    return false;
                }
            }

            if (model.OpcSamplingInterval != that.OpcSamplingInterval) {
                return false;
            }

            if (model.HeartbeatIntervalTimespan.GetTimeSpanFromSeconds(model.HeartbeatInterval) !=
                that.HeartbeatIntervalTimespan.GetTimeSpanFromSeconds(that.HeartbeatInterval)) {
                return false;
            }

            if (model.SkipFirst != that.SkipFirst) {
                return false;
            }

            if (model.QueueSize != that.QueueSize) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the hashcode for a node
        /// </summary>
        public static int GetHashCode(this OpcNodeModel model) {
            var hash = new HashCode();
            hash.Add(model.Id);
            hash.Add(model.DisplayName);
            hash.Add(model.DataSetFieldId);
            hash.Add(model.ExpandedNodeId);
            hash.Add(model.OpcPublishingInterval);
            hash.Add(model.OpcSamplingInterval);
            hash.Add(model.HeartbeatIntervalTimespan.GetTimeSpanFromSeconds(model.HeartbeatInterval));
            hash.Add(model.SkipFirst);
            hash.Add(model.QueueSize);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Returns a the timespan value from the timespan repsecively integer rperesenting seconds
        /// The Timespan value wins when provided
        /// </summary>
        public static TimeSpan?  GetTimeSpanFromSeconds(this TimeSpan? timespan, int? seconds) {

            return timespan.HasValue
                ? timespan
                : seconds.HasValue
                    ? TimeSpan.FromSeconds(seconds.Value)
                    : (TimeSpan?)null;
        }

        /// <summary>
        /// Equality comparer for OpcNodeModel objects.
        /// </summary>
        private class OpcNodeModelComparer : EqualityComparer<OpcNodeModel> {

            /// <inheritdoc/>
            public override bool Equals(OpcNodeModel node1, OpcNodeModel node2) {
                return node1.IsSame(node2);
            }

            /// <inheritdoc/>
            public override int GetHashCode(OpcNodeModel node) {
                return OpcNodeModelEx.GetHashCode(node);
            }
        }
    }
}
