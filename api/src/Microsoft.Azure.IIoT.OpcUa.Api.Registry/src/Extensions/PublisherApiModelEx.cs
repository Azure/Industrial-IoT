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
        public static PublisherApiModel Patch(this PublisherApiModel update,
            PublisherApiModel publisher) {
            if (update == null) {
                return publisher;
            }
            if (publisher == null) {
                publisher = new PublisherApiModel();
            }
            publisher.Connected = update.Connected;
            publisher.Id = update.Id;
            publisher.LogLevel = update.LogLevel;
            publisher.OutOfSync = update.OutOfSync;
            publisher.SiteId = update.SiteId;
            publisher.Configuration = (update.Configuration ?? new PublisherConfigApiModel())
                .Patch(publisher.Configuration);
            return publisher;
        }
    }
}
