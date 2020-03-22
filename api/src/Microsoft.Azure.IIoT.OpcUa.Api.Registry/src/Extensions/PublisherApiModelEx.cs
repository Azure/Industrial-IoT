// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    /// <summary>
    /// Handle event
    /// </summary>
    public static class PublisherApiModelEx {

        /// <summary>
        /// Update a discover
        /// </summary>
        /// <param name="publisher"></param>
        /// <param name="update"></param>
        /// <param name="isPatch"></param>
        public static PublisherApiModel Patch(this PublisherApiModel update,
            PublisherApiModel publisher, bool isPatch = false) {
            if (publisher == null) {
                return update;
            }
            if (!isPatch || update.Connected != null) {
                publisher.Connected = update.Connected;
            }
            if (!isPatch || update.Id != null) {
                publisher.Id = update.Id;
            }
            if (!isPatch || update.LogLevel != null) {
                publisher.LogLevel = update.LogLevel;
            }
            if (!isPatch || update.OutOfSync != null) {
                publisher.OutOfSync = update.OutOfSync;
            }
            if (!isPatch || update.SiteId != null) {
                publisher.SiteId = update.SiteId;
            }
            publisher.Configuration = update.Configuration.Patch(
                publisher.Configuration, isPatch);
            return publisher;
        }
    }
}
