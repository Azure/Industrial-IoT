// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Publisher configuration
    /// </summary>
    public interface IPublisherConfiguration
    {
        /// <summary>
        /// Site of the publisher
        /// </summary>
        string Site { get; }

        /// <summary>
        /// Configuration file
        /// </summary>
        string PublishedNodesFile { get; }

        /// <summary>
        /// Configuration schema file
        /// </summary>
        string PublishedNodesSchemaFile { get; }

        /// <summary>
        /// Max number of nodes per publish endpoint
        /// </summary>
        int MaxNodesPerPublishedEndpoint { get; }

        /// <summary>
        /// Messaging profile to use
        /// </summary>
        MessagingProfile MessagingProfile { get; }

        /// <summary>
        /// Configuration flag for enabling/disabling
        /// runtime state reporting.
        /// </summary>
        bool EnableRuntimeStateReporting { get; }

        /// <summary>
        /// The routing info to add to the runtime state
        /// events.
        /// </summary>
        string RuntimeStateRoutingInfo { get; }

        /// <summary>
        /// Scale test option
        /// </summary>
        int? ScaleTestCount { get; }
    }
}
