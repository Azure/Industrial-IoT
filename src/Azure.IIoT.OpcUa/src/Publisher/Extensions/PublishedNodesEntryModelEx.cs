// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Config.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    /// <summary>
    /// PublishedNodesEntryModel extensions
    /// </summary>
    public static class PublishedNodesEntryModelEx
    {
        /// <summary>
        /// Get unique identifier for the group
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GetUniqueWriterGroupId(this PublishedNodesEntryModel model)
        {
            var id = new StringBuilder();
            if (!string.IsNullOrEmpty(model.DataSetWriterGroup))
            {
                id.Append(model.DataSetWriterGroup);
            }
            if (model.WriterGroupTransport != null)
            {
                id.Append(model.WriterGroupTransport);
            }
            if (model.WriterGroupQualityOfService != null)
            {
                id.Append(model.WriterGroupQualityOfService.Value);
            }
            if (!string.IsNullOrEmpty(model.WriterGroupQueueName))
            {
                id.Append(model.WriterGroupQueueName);
            }
            if (model.MessageEncoding != null)
            {
                id.Append(model.MessageEncoding.Value);
            }
            if (model.MessagingMode != null)
            {
                id.Append(model.MessagingMode.Value);
            }
            if (model.BatchSize != null)
            {
                id.Append(model.BatchSize.Value);
            }
            var batchTriggerInterval = model.GetNormalizedBatchTriggerInterval();
            if (batchTriggerInterval != null)
            {
                id.Append(batchTriggerInterval.Value.TotalMilliseconds);
            }
            if (model.WriterGroupPartitions != null)
            {
                id.Append(model.WriterGroupPartitions.Value);
            }
            return id.ToString().ToSha1Hash();
        }

        /// <summary>
        /// Validates if the entry has same group as the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        public static bool HasSameWriterGroup(this PublishedNodesEntryModel model,
            PublishedNodesEntryModel that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (!string.Equals(model.DataSetWriterGroup,
                that.DataSetWriterGroup, StringComparison.Ordinal))
            {
                return false;
            }
            if (model.WriterGroupTransport != that.WriterGroupTransport)
            {
                return false;
            }
            if (model.WriterGroupQualityOfService != that.WriterGroupQualityOfService)
            {
                return false;
            }
            if (!string.Equals(model.WriterGroupQueueName ?? string.Empty,
                that.WriterGroupQueueName ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
            if (model.MessageEncoding != that.MessageEncoding)
            {
                return false;
            }
            if (model.MessagingMode != that.MessagingMode)
            {
                return false;
            }
            if (model.BatchSize != that.BatchSize)
            {
                return false;
            }
            if (model.GetNormalizedBatchTriggerInterval() !=
                that.GetNormalizedBatchTriggerInterval())
            {
                return false;
            }
            if (model.WriterGroupPartitions != that.WriterGroupPartitions)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create a new published nodes entry model. This is used only for the legacy
        /// API to start, stop, bulk and list nodes. If the connection model uses the
        /// group field it is used as writer group identifier.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishedNodesEntryModel? ToPublishedNodesEntry(this ConnectionModel model)
        {
            if (model is null)
            {
                return null;
            }

            var useSecurity =
                model.Endpoint?.SecurityMode == SecurityMode.None ? false :
                model.Endpoint?.SecurityMode == SecurityMode.NotNone ? true :
                (bool?)null;

            return new PublishedNodesEntryModel
            {
                EndpointUrl = model.Endpoint?.Url,
                UseSecurity = useSecurity,
                EndpointSecurityMode = !useSecurity.HasValue ? model.Endpoint?.SecurityMode : null,
                EndpointSecurityPolicy = model.Endpoint?.SecurityPolicy,
                OpcAuthenticationMode = ToAuthenticationModel(model.User?.Type),
                OpcAuthenticationPassword = model.User.GetPassword(),
                OpcAuthenticationUsername = model.User.GetUserName(),
                DataSetWriterGroup = model.Group,
                UseReverseConnect =
                    model.Options.HasFlag(ConnectionOptions.UseReverseConnect) ? true : null,
                DisableSubscriptionTransfer =
                    model.Options.HasFlag(ConnectionOptions.NoSubscriptionTransfer) ? true : null,
                MessageEncoding = MessageEncoding.Json,
                MessagingMode = MessagingMode.FullSamples,
                OpcNodes = new List<OpcNodeModel>()
            };
        }

        /// <summary>
        /// Convert to mode
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static OpcAuthenticationMode ToAuthenticationModel(this CredentialType? type)
        {
            switch (type)
            {
                case CredentialType.UserName:
                    return OpcAuthenticationMode.UsernamePassword;
                case CredentialType.X509Certificate:
                    return OpcAuthenticationMode.Certificate;
                default:
                    return OpcAuthenticationMode.Anonymous;
            }
        }

        /// <summary>
        /// Return a cloaked published nodes entry that can be used as lookup input to
        /// <see cref="HasSameDataSet(PublishedNodesEntryModel, PublishedNodesEntryModel)"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishedNodesEntryModel? ToDataSetEntry(this PublishedNodesEntryModel? model)
        {
            if (model is null)
            {
                return null;
            }
            return model with
            {
                NodeId = null,
                EncryptedAuthPassword = null,
                OpcAuthenticationPassword = null,
                OpcNodes = null
            };
        }

        /// <summary>
        /// Get a unique data set writer id from the entry model. Excludes the
        /// writer group which is assumed be scoping this id already.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="publishingInterval"></param>
        /// <returns></returns>
        public static string GetUniqueDataSetWriterId(this PublishedNodesEntryModel model,
            TimeSpan? publishingInterval = null)
        {
            var id = new StringBuilder();
            if (!string.IsNullOrEmpty(model.DataSetWriterId))
            {
                id.Append(model.DataSetWriterId);
            }
            if (!string.IsNullOrEmpty(model.EndpointUrl))
            {
                id.Append(model.EndpointUrl);
            }
            if (model.UseReverseConnect == true)
            {
                id.AppendLine();
            }
            if (model.DisableSubscriptionTransfer == true)
            {
                id.AppendLine();
            }
            var securityMode = model.EndpointSecurityMode ??
                ((model.UseSecurity ?? false) ? SecurityMode.NotNone : SecurityMode.None);
            if (securityMode != SecurityMode.None)
            {
                id.Append(securityMode);
            }
            if (!string.IsNullOrEmpty(model.EndpointSecurityPolicy))
            {
                id.Append(model.EndpointSecurityPolicy);
            }
            if (model.OpcAuthenticationMode != OpcAuthenticationMode.Anonymous)
            {
                id.Append(model.OpcAuthenticationMode);
            }
            if (!string.IsNullOrEmpty(model.OpcAuthenticationUsername))
            {
                id.Append(model.OpcAuthenticationUsername);
            }
            if (!string.IsNullOrEmpty(model.OpcAuthenticationPassword))
            {
                id.Append(model.OpcAuthenticationPassword.ToSha1Hash());
            }
            if (!string.IsNullOrEmpty(model.EncryptedAuthUsername))
            {
                id.Append(model.EncryptedAuthUsername);
            }
            if (!string.IsNullOrEmpty(model.EncryptedAuthPassword))
            {
                id.Append(model.EncryptedAuthPassword.ToSha1Hash());
            }
            if (!string.IsNullOrEmpty(model.DataSetName))
            {
                id.Append(model.DataSetName);
            }
            var publishingIntervalResolved = publishingInterval ??
                model.GetNormalizedDataSetPublishingInterval();
            if (publishingIntervalResolved != null)
            {
                id.Append(publishingIntervalResolved.Value.TotalMilliseconds);
            }
            if (model.DataSetClassId != Guid.Empty)
            {
                id.Append(model.DataSetClassId);
            }
            if (model.DataSetKeyFrameCount != null)
            {
                id.Append(model.DataSetKeyFrameCount.Value);
            }
            if (model.DisableSubscriptionTransfer != null)
            {
                id.Append(model.DisableSubscriptionTransfer.Value);
            }
            if (model.SendKeepAliveDataSetMessages)
            {
                id.AppendLine();
            }
            if (model.Priority != null)
            {
                id.Append(model.Priority.Value);
            }
            var metadataUpdateTime = model.GetNormalizedMetaDataUpdateTime();
            if (metadataUpdateTime != null)
            {
                id.Append(metadataUpdateTime.Value.TotalMilliseconds);
            }
            var samplingInterval = model.GetNormalizedDataSetSamplingInterval();
            if (samplingInterval != null)
            {
                id.Append(samplingInterval.Value.TotalMilliseconds);
            }
            if (model.QualityOfService != null)
            {
                id.Append(model.QualityOfService.Value);
            }
            if (!string.IsNullOrEmpty(model.QueueName))
            {
                id.Append(model.QueueName);
            }
            if (!string.IsNullOrEmpty(model.MetaDataQueueName))
            {
                id.Append(model.MetaDataQueueName);
            }
            if ((model.DataSetRouting ?? DataSetRoutingMode.None)
                != DataSetRoutingMode.None)
            {
                id.Append(model.DataSetRouting.ToString());
            }
            if (model.RepublishAfterTransfer != null)
            {
                id.Append(model.RepublishAfterTransfer.Value);
            }
            if (model.OpcNodeWatchdogTimespan != null)
            {
                id.Append(model.OpcNodeWatchdogTimespan.Value);
            }
            if (model.DataSetWriterWatchdogBehavior != null)
            {
                id.Append(model.DataSetWriterWatchdogBehavior.Value);
            }
            if (model.OpcNodeWatchdogCondition != null)
            {
                id.Append(model.OpcNodeWatchdogCondition.Value);
            }
            Debug.Assert(id.Length != 0); // Should always have an endpoint mixed in
            return id.ToString().ToSha1Hash();
        }

        /// <summary>
        /// Validates if the entry has same data set definition as the model.
        /// Comarison excludes OpcNodes and publishing intervals.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        public static bool HasSameDataSet(this PublishedNodesEntryModel model,
            PublishedNodesEntryModel that)
        {
            if (!model.HasSameWriterGroup(that))
            {
                return false;
            }

            if (model.EndpointUrl != that.EndpointUrl)
            {
                return false;
            }
            if ((model.UseReverseConnect ?? false) !=
                (that.UseReverseConnect ?? false))
            {
                return false;
            }
            if ((model.DisableSubscriptionTransfer ?? false) !=
                (that.DisableSubscriptionTransfer ?? false))
            {
                return false;
            }
            if ((model.UseSecurity ?? false) != (that.UseSecurity ?? false))
            {
                return false;
            }
            if ((model.EndpointSecurityMode ??
                    ((model.UseSecurity ?? false) ? SecurityMode.NotNone : SecurityMode.None)) !=
                (that.EndpointSecurityMode ??
                    ((that.UseSecurity ?? false) ? SecurityMode.NotNone : SecurityMode.None)))
            {
                return false;
            }
            if (model.EndpointSecurityPolicy != that.EndpointSecurityPolicy)
            {
                return false;
            }
            if (model.OpcAuthenticationMode != that.OpcAuthenticationMode)
            {
                return false;
            }
            if (!string.Equals(model.OpcAuthenticationUsername ?? string.Empty,
                that.OpcAuthenticationUsername ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
            if (!string.Equals(model.OpcAuthenticationPassword ?? string.Empty,
                that.OpcAuthenticationPassword ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
            if (!string.Equals(model.EncryptedAuthUsername ?? string.Empty,
                that.EncryptedAuthUsername ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
            if (!string.Equals(model.EncryptedAuthPassword ?? string.Empty,
                that.EncryptedAuthPassword ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(model.DataSetWriterId ?? string.Empty,
                that.DataSetWriterId ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
            if (!string.Equals(model.DataSetName ?? string.Empty,
                that.DataSetName ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
            if (model.DataSetClassId != that.DataSetClassId)
            {
                return false;
            }
            if (model.DataSetKeyFrameCount != that.DataSetKeyFrameCount)
            {
                return false;
            }
            if (model.DisableSubscriptionTransfer != that.DisableSubscriptionTransfer)
            {
                return false;
            }
            if (model.SendKeepAliveDataSetMessages != that.SendKeepAliveDataSetMessages)
            {
                return false;
            }
            if (model.Priority != that.Priority)
            {
                return false;
            }
            if (model.GetNormalizedMetaDataUpdateTime() !=
                that.GetNormalizedMetaDataUpdateTime())
            {
                return false;
            }
            if (model.GetNormalizedDataSetSamplingInterval() !=
                that.GetNormalizedDataSetSamplingInterval())
            {
                return false;
            }
            if (model.QualityOfService != that.QualityOfService)
            {
                return false;
            }
            if (!string.Equals(model.QueueName ?? string.Empty,
                that.QueueName ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
            if (!string.Equals(model.MetaDataQueueName ?? string.Empty,
                that.MetaDataQueueName ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }

            if ((model.DataSetRouting ?? DataSetRoutingMode.None) !=
                (that.DataSetRouting ?? DataSetRoutingMode.None))
            {
                return false;
            }
            if (model.RepublishAfterTransfer != that.RepublishAfterTransfer)
            {
                return false;
            }
            if (model.OpcNodeWatchdogTimespan != that.OpcNodeWatchdogTimespan)
            {
                return false;
            }
            if (model.DataSetWriterWatchdogBehavior != that.DataSetWriterWatchdogBehavior)
            {
                return false;
            }
            if (model.OpcNodeWatchdogCondition != that.OpcNodeWatchdogCondition)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieves the timespan flavor of a PublishedNodesEntryModel's MetaDataUpdateTime
        /// </summary>
        /// <param name="model"></param>
        public static TimeSpan? GetNormalizedMetaDataUpdateTime(
            this PublishedNodesEntryModel model)
        {
            return model.MetaDataUpdateTimeTimespan
                .GetTimeSpanFromMiliseconds(model.MetaDataUpdateTime);
        }

        /// <summary>
        /// Retrieves the timespan flavor of a PublishedNodesEntryModel's BatchTriggerInterval
        /// </summary>
        /// <param name="model"></param>
        public static TimeSpan? GetNormalizedBatchTriggerInterval(
            this PublishedNodesEntryModel model)
        {
            return model.BatchTriggerIntervalTimespan
                .GetTimeSpanFromMiliseconds(model.BatchTriggerInterval);
        }

        /// <summary>
        /// Retrieves the timespan flavor of a PublishedNodesEntryModel's SamplingInterval
        /// </summary>
        /// <param name="model"></param>
        public static TimeSpan? GetNormalizedDataSetSamplingInterval(
            this PublishedNodesEntryModel model)
        {
            return model.DataSetSamplingIntervalTimespan
                .GetTimeSpanFromMiliseconds(model.DataSetSamplingInterval);
        }

        /// <summary>
        /// Retrieves the timespan flavor of a PublishedNodesEntryModel's DataSetPublishingInterval
        /// </summary>
        /// <param name="model"></param>
        /// <param name="defaultPublishingTimespan"></param>
        public static TimeSpan? GetNormalizedDataSetPublishingInterval(
            this PublishedNodesEntryModel model, TimeSpan? defaultPublishingTimespan = null)
        {
            return model.DataSetPublishingIntervalTimespan
                .GetTimeSpanFromMiliseconds(model.DataSetPublishingInterval, defaultPublishingTimespan);
        }

        /// <summary>
        /// Promote the default publishing interval of the model to all of
        /// its nodes to support apples to apples comparison.
        /// </summary>
        /// <param name="model"></param>
        public static PublishedNodesEntryModel PropagatePublishingIntervalToNodes(
            this PublishedNodesEntryModel model)
        {
            if (model.OpcNodes != null && model.OpcNodes.Count != 0)
            {
                var rootInterval = model.GetNormalizedDataSetPublishingInterval();
                if (rootInterval == null)
                {
                    return model;
                }
                foreach (var node in model.OpcNodes)
                {
                    var nodeInterval = node.GetNormalizedPublishingInterval();
                    if (nodeInterval == null)
                    {
                        // Set publishing interval from root
                        node.OpcPublishingIntervalTimespan = rootInterval;
                    }
                }
            }
            // Remove root interval
            model.DataSetPublishingInterval = null;
            model.DataSetPublishingIntervalTimespan = null;
            return model;
        }
    }
}
