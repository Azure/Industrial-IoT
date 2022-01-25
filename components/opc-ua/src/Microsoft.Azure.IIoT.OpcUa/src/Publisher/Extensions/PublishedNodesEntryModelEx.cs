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

            if (string.Compare(model.EndpointUrl.OriginalString, that.EndpointUrl.OriginalString, StringComparison.OrdinalIgnoreCase) != 0) {
                return false;
            }

            if (model.UseSecurity != that.UseSecurity) {
                return false;
            }

            if (model.OpcAuthenticationMode != that.OpcAuthenticationMode && that.OpcAuthenticationMode != OpcAuthenticationMode.Anonymous) {
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
        /// filter model
        /// </summary>
        public static bool IsFiltered(this PublishedNodesEntryModel model, PublishedNodesEntryModel that) {

            if (model == that) {
                return true;
            }

            if (model == null || that == null) {
                return false;
            }

            if (string.Compare(model.DataSetWriterGroup, that.DataSetWriterGroup, StringComparison.InvariantCulture) != 0 &&
                that.DataSetWriterGroup != null) {
                return false;
            }

            if (string.Compare(model.DataSetWriterId, that.DataSetWriterId, StringComparison.InvariantCulture) != 0 &&
                that.DataSetWriterId != null) {
                return false;
            }

            if (string.Compare(model.EndpointUrl.OriginalString, that.EndpointUrl.OriginalString, StringComparison.OrdinalIgnoreCase) != 0) {
                return false;
            }

            if (model.DataSetPublishingInterval != that.DataSetPublishingInterval && that.DataSetPublishingInterval != null) {
                return false;
            }

            if (model.UseSecurity != that.UseSecurity && that.UseSecurity != null) {
                return false;
            }

            if (model.OpcAuthenticationMode != that.OpcAuthenticationMode && that.OpcAuthenticationMode != OpcAuthenticationMode.Anonymous) {
                return false;
            }

            if (string.Compare(model.OpcAuthenticationUsername, that.OpcAuthenticationUsername, StringComparison.InvariantCulture) != 0 &&
                that.OpcAuthenticationUsername != null) {
                return false;
            }

            if (string.Compare(model.OpcAuthenticationPassword, that.OpcAuthenticationPassword, StringComparison.InvariantCulture) != 0 &&
                that.OpcAuthenticationPassword != null) {
                return false;
            }

            if (string.Compare(model.EncryptedAuthUsername, that.EncryptedAuthUsername, StringComparison.InvariantCulture) != 0 &&
                that.EncryptedAuthUsername != null) {
                return false;
            }

            if (string.Compare(model.EncryptedAuthPassword, that.EncryptedAuthPassword, StringComparison.InvariantCulture) != 0 &&
                that.EncryptedAuthPassword != null) {
                return false;
            }

            return true;
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
    }
}