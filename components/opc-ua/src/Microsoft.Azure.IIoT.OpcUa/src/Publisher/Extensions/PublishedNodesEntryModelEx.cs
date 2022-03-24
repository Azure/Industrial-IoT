// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models {
    using System;

    /// <summary>
    /// PublishedNodesEntryModel extensions
    /// </summary>
    public static class PublishedNodesEntryModelEx {

        /// <summary>
        /// Validates if the entry has same group as the model
        /// </summary>
        public static bool HasSameGroup(this PublishedNodesEntryModel model, PublishedNodesEntryModel that) {

            if (model == that) {
                return true;
            }

            if (model == null || that == null) {
                return false;
            }

            if (string.Compare(model.DataSetWriterGroup, that.DataSetWriterGroup, StringComparison.InvariantCulture) != 0) {
                return false;
            }

            if (model.EndpointUrl != that.EndpointUrl) {
                return false;
            }

            if (model.UseSecurity != that.UseSecurity) {
                return false;
            }

            if (model.OpcAuthenticationMode != that.OpcAuthenticationMode) {
                return false;
            }

            if (string.Compare(model.OpcAuthenticationUsername, that.OpcAuthenticationUsername, StringComparison.InvariantCulture) != 0) {
                return false;
            }

            if (string.Compare(model.OpcAuthenticationPassword, that.OpcAuthenticationPassword, StringComparison.InvariantCulture) != 0) {
                return false;
            }

            if (string.Compare(model.EncryptedAuthUsername, that.EncryptedAuthUsername, StringComparison.InvariantCulture) != 0) {
                return false;
            }

            if (string.Compare(model.EncryptedAuthPassword, that.EncryptedAuthPassword, StringComparison.InvariantCulture) != 0) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates if the entry has same data set definition as the model. Comarison excludes OpcNodes.
        /// </summary>
        public static bool HasSameDataSet(this PublishedNodesEntryModel model, PublishedNodesEntryModel that) {
            return model.HasSameGroup(that)
                && string.Equals(model.DataSetWriterId, that.DataSetWriterId, StringComparison.InvariantCulture)
                && model.GetNormalizedDataSetPublishingInterval() == that.GetNormalizedDataSetPublishingInterval();
        }

        /// <summary>
        /// Returns the GroupId if available otherwise a valid identifier cmposed of endpoint url and hash
        /// </summary>
        public static string GetGroupId(this PublishedNodesEntryModel model) {
            if (model == null) {
                return string.Empty;
            }
            return !string.IsNullOrEmpty(model.DataSetWriterGroup) ?
                   model.DataSetWriterGroup :
                   $"{model.EndpointUrl.OriginalString}_{model.GetGroupHashCode()}";
        }

        /// <summary>
        /// Returns the hashcode for a group
        /// </summary>
        public static int GetGroupHashCode(this PublishedNodesEntryModel model) {

            return HashCode.Combine(
                model.EndpointUrl,
                model.UseSecurity,
                model.OpcAuthenticationMode,
                model.OpcAuthenticationUsername,
                model.OpcAuthenticationPassword,
                model.EncryptedAuthUsername,
                model.EncryptedAuthPassword);
        }

        /// <summary>
        /// Retrieves the timespan flavor of a PublishedNodesEntryModel's DataSetPublishingInterval
        /// </summary>
        public static TimeSpan? GetNormalizedDataSetPublishingInterval(
            this PublishedNodesEntryModel model, TimeSpan? defaultPublishingTimespan = null) {
            return model.DataSetPublishingIntervalTimespan
                .GetTimeSpanFromMiliseconds(model.DataSetPublishingInterval, defaultPublishingTimespan);
        }
    }
}
