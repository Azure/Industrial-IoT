// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher {
    using Azure.IIoT.OpcUa.Shared.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher
    /// </summary>
    public interface IPublisherHost {
        /// <summary>
        /// Current version number of the configuration
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Last change time
        /// </summary>
        DateTime LastChange { get; }

        /// <summary>
        /// Current list of writer groups in publisher
        /// </summary>
        IEnumerable<WriterGroupJobModel> WriterGroups { get; }

        /// <summary>
        /// Try update configuration
        /// </summary>
        /// <param name="jobs"></param>
        /// <returns></returns>
        bool TryUpdate(IEnumerable<WriterGroupJobModel> jobs);

        /// <summary>
        /// Update configuration
        /// </summary>
        /// <param name="jobs"></param>
        /// <returns></returns>
        Task UpdateAsync(IEnumerable<WriterGroupJobModel> jobs);
    }
}