//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api {
    using Microsoft.Azure.IIoT.Api.Identity.Clients;
    using Microsoft.Azure.IIoT.Api.Jobs.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Clients;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    /// <summary>
    /// Helper
    /// </summary>
    public class TypeFixture {

        public static IEnumerable<object[]> GetDataContractTypes() {
            return      GetAllApiModelTypes<BrowseDirection>()
                .Concat(GetAllApiModelTypes<IdentityServiceClient>())
                .Concat(GetAllApiModelTypes<VaultServiceClient>())
                .Concat(GetAllApiModelTypes<TwinServiceClient>())
                .Concat(GetAllApiModelTypes<RegistryServiceClient>())
                .Concat(GetAllApiModelTypes<PublisherServiceClient>())
                .Concat(GetAllApiModelTypes<HistoryServiceClient>())
                .Concat(GetAllApiModelTypes<JobsServiceClient>())
                .Distinct()
                .Select(t => new object[] { t });
        }

        public static IEnumerable<Type> GetAllApiModelTypes<T>() {
            return typeof(T).Assembly.GetExportedTypes()
                .Where(t => t.GetCustomAttribute<DataContractAttribute>() != null
                    && t.GetGenericArguments().Length == 0);
        }
    }
}
