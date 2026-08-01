// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Splits a writer group whose writers publish to different destinations
    /// into one group per destination.
    /// </summary>
    /// <remarks>
    /// A native writer group is one network message on one topic. The writer
    /// path does not work that way: routing modes such as the unified
    /// namespace turn a single dataset into one writer per item, each with its
    /// own resolved queue name, and leave them all in the same group. The
    /// native egress cannot carry that - it refused outright rather than
    /// silently publishing them all to one topic - so the group is separated
    /// here instead, before anything downstream sees it.
    ///
    /// A group whose writers all agree, which is every group that is not using
    /// a routing mode, is passed through unchanged and keeps its identity. That
    /// matters: writer group ids key the durable identity registry, so a
    /// scheme that renamed groups gratuitously would change identities across
    /// an upgrade. Groups that do disagree have never worked on this path at
    /// all, so they have no identity to preserve.
    /// </remarks>
    internal static class WriterGroupDestinationSplitter
    {
        /// <summary>
        /// Separate the writer groups so that every writer in a group shares
        /// the group's destination.
        /// </summary>
        /// <param name="writerGroups">The groups to separate.</param>
        public static IReadOnlyList<WriterGroupModel> Split(
            IReadOnlyList<WriterGroupModel> writerGroups)
        {
            ArgumentNullException.ThrowIfNull(writerGroups);
            if (!writerGroups.Any(NeedsSplitting))
            {
                return writerGroups;
            }
            var result = new List<WriterGroupModel>(writerGroups.Count);
            foreach (var writerGroup in writerGroups)
            {
                if (!NeedsSplitting(writerGroup))
                {
                    result.Add(writerGroup);
                    continue;
                }
                //
                // Ordered by the derived suffix rather than by encounter, so
                // that the same configuration always produces the same groups
                // in the same order however the writers happen to be listed.
                //
                var partitions = writerGroup.DataSetWriters!
                    .GroupBy(writer => writer.Publishing ?? writerGroup.Publishing,
                        DestinationComparer.Instance)
                    .Select(partition => (
                        Suffix: DeriveSuffix(partition.Key),
                        Destination: partition.Key,
                        Writers: partition.ToList()))
                    .OrderBy(partition => partition.Suffix, StringComparer.Ordinal)
                    .ToList();
                foreach (var (suffix, destination, writers) in partitions)
                {
                    result.Add(writerGroup with
                    {
                        Id = writerGroup.Id + "_" + suffix,
                        Publishing = destination,
                        DataSetWriters = writers
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// True when the group's writers do not all publish to the same place.
        /// </summary>
        /// <param name="writerGroup"></param>
        private static bool NeedsSplitting(WriterGroupModel writerGroup)
        {
            var writers = writerGroup.DataSetWriters;
            if (writers is null || writers.Count <= 1)
            {
                return false;
            }
            var first = writers[0].Publishing ?? writerGroup.Publishing;
            return writers.Skip(1).Any(writer => !DestinationComparer.Instance.Equals(
                writer.Publishing ?? writerGroup.Publishing, first));
        }

        /// <summary>
        /// Derive a stable suffix for a destination.
        /// </summary>
        /// <remarks>
        /// Derived from the destination itself rather than from a counter, so
        /// that adding, removing or reordering writers does not renumber the
        /// groups that did not change - which would otherwise churn every
        /// durable identity in the group on an unrelated edit.
        /// </remarks>
        /// <param name="destination"></param>
        private static string DeriveSuffix(PublishingQueueSettingsModel? destination)
        {
            if (destination is null)
            {
                return "default";
            }
            var identity = string.Join('\u001f',
                destination.QueueName ?? string.Empty,
                destination.RequestedDeliveryGuarantee?.ToString() ?? string.Empty,
                destination.Retain?.ToString() ?? string.Empty,
                destination.Ttl?.ToString() ?? string.Empty);
            return ((uint)StringComparer.Ordinal.GetHashCode(identity))
                .ToString("x8", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Compares destinations the way the egress does, so that the split
        /// produces exactly the groups the egress would otherwise reject.
        /// </summary>
        private sealed class DestinationComparer :
            IEqualityComparer<PublishingQueueSettingsModel?>
        {
            public static DestinationComparer Instance { get; } = new();

            public bool Equals(PublishingQueueSettingsModel? x,
                PublishingQueueSettingsModel? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (x is null || y is null)
                {
                    return false;
                }
                return string.Equals(x.QueueName, y.QueueName, StringComparison.Ordinal)
                    && x.RequestedDeliveryGuarantee == y.RequestedDeliveryGuarantee
                    && x.Retain == y.Retain
                    && x.Ttl == y.Ttl;
            }

            public int GetHashCode(PublishingQueueSettingsModel? obj)
            {
                if (obj is null)
                {
                    return 0;
                }
                return HashCode.Combine(
                    obj.QueueName is null ? 0 :
                        StringComparer.Ordinal.GetHashCode(obj.QueueName),
                    obj.RequestedDeliveryGuarantee,
                    obj.Retain,
                    obj.Ttl);
            }
        }
    }
}
