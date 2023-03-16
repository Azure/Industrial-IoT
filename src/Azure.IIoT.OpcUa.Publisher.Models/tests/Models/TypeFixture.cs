//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    /// <summary>
    /// Helper
    /// </summary>
    public static class TypeFixture
    {
        public static IEnumerable<object[]> GetDataContractTypes()
        {
            return GetAllModelTypes<BrowseDirection>()
                .Distinct()
                .Select(t => new object[] { t });
        }

        public static IEnumerable<Type> GetAllModelTypes<T>()
        {
            return typeof(T).Assembly.GetExportedTypes()
                .Where(t => t.GetCustomAttribute<DataContractAttribute>() != null
                    && t.GetGenericArguments().Length == 0);
        }
    }
}
