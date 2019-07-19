// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.ServiceBus {
    using System.Threading.Tasks;

    /// <summary>
    /// A managed service bus resource
    /// </summary>
    public interface IServiceBusResource : IResource {

        /// <summary>
        /// The service bus manage connection string
        /// </summary>
        string PrimaryManageConnectionString { get; }

        /// <summary>
        /// The service bus manage connection string
        /// </summary>
        string SecondaryManageConnectionString { get; }

        /// <summary>
        /// The service bus send connection string
        /// </summary>
        string PrimarySendConnectionString { get; }

        /// <summary>
        /// The service bus send connection string
        /// </summary>
        string SecondarySendConnectionString { get; }

        /// <summary>
        /// The service bus listen connection string
        /// </summary>
        string PrimaryListenConnectionString { get; }

        /// <summary>
        /// The service bus listen connection string
        /// </summary>
        string SecondaryListenConnectionString { get; }

        /// <summary>
        /// Create service bus topic
        /// </summary>
        /// <param name="name"></param>
        /// <param name="maxSizeInMb"></param>
        /// <returns></returns>
        Task CreateTopicAsync(string name, int? maxSizeInMb);

        /// <summary>
        /// Delete topic
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task DeleteTopicAsync(string name);

        /// <summary>
        /// Create service bus queue
        /// </summary>
        /// <param name="name"></param>
        /// <param name="maxSizeInMb"></param>
        /// <returns></returns>
        Task CreateQueueAsync(string name, int? maxSizeInMb);

        /// <summary>
        /// Delete queue
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task DeleteQueueAsync(string name);
    }
}
