// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Config.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// PublishedNodesEntryModel extensions
    /// </summary>
    public static class PublishedNodesEntryModelEx
    {
        /// <summary>
        /// Validates if the entry has same group as the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        public static bool HasSameGroup(this PublishedNodesEntryModel model,
            PublishedNodesEntryModel that)
        {
            if (model == that)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            if (!string.Equals(model.DataSetWriterGroup,
                that.DataSetWriterGroup, StringComparison.Ordinal))
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
            if (model == null)
            {
                return null;
            }
            return new PublishedNodesEntryModel
            {
                EndpointUrl = model.Endpoint?.Url,
                UseSecurity = model.Endpoint?.SecurityMode != SecurityMode.None,
                EndpointSecurityMode = model.Endpoint?.SecurityMode,
                EndpointSecurityPolicy = model.Endpoint?.SecurityPolicy,
                OpcAuthenticationMode = ToAuthenticationModel(model.User?.Type),
                OpcAuthenticationPassword = model.User.GetPassword(),
                OpcAuthenticationUsername = model.User.GetUserName(),
                DataSetWriterGroup = model.Group,
                UseReverseConnect = model.IsReverse,
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
        /// Check if has same endpoint
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool HasSameEndpoint(this PublishedNodesEntryModel model,
            PublishedNodesEntryModel that)
        {
            if (model == that)
            {
                return true;
            }
            if (model == null || that == null)
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
            if (model.UseSecurity != that.UseSecurity)
            {
                return false;
            }
            if ((model.EndpointSecurityMode ??
                    (model.UseSecurity ? SecurityMode.SignAndEncrypt : SecurityMode.None)) !=
                (that.EndpointSecurityMode ??
                    (that.UseSecurity ? SecurityMode.SignAndEncrypt : SecurityMode.None)))
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
            return true;
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
            if (model == null)
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
        /// Validates if the entry has same data set definition as the model.
        /// Comarison excludes OpcNodes.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        public static bool HasSameDataSet(this PublishedNodesEntryModel model,
            PublishedNodesEntryModel that)
        {
            if (!string.Equals(model.DataSetWriterId ?? string.Empty,
                that.DataSetWriterId ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }
            if (!model.HasSameGroup(that))
            {
                return false;
            }
            if (!model.HasSameEndpoint(that))
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
            if (model.WriterGroupTransport != that.WriterGroupTransport)
            {
                return false;
            }
            if (model.WriterGroupQualityOfService != that.WriterGroupQualityOfService)
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
            if (model.MessageEncoding != that.MessageEncoding)
            {
                return false;
            }
            if (model.MessagingMode != that.MessagingMode)
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
