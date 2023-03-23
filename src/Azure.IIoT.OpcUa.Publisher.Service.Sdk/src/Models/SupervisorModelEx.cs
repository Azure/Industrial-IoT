// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Handle event
    /// </summary>
    public static class SupervisorModelEx
    {
        /// <summary>
        /// Update a discover
        /// </summary>
        /// <param name="update"></param>
        /// <param name="supervisor"></param>
        public static SupervisorModel Patch(this SupervisorModel update,
            SupervisorModel supervisor)
        {
            if (update == null)
            {
                return supervisor;
            }
            supervisor ??= new SupervisorModel();
            supervisor.Connected = update.Connected;
            supervisor.Id = update.Id;
            supervisor.OutOfSync = update.OutOfSync;
            supervisor.SiteId = update.SiteId;
            return supervisor;
        }
    }
}
