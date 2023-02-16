//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api {
    using Microsoft.Azure.IIoT.Api.Clients;
    using Microsoft.Azure.IIoT.Api.Models;
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
            return GetAllApiModelTypes<ApplicationInfoModel>()
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
