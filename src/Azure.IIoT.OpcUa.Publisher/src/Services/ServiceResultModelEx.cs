// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Service result model extensions
    /// </summary>
    internal static class ServiceResultModelEx
    {
        /// <summary>
        /// Restore a symbolic id lost when a status code is decoded.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static ServiceResultModel WithSymbolicId(this ServiceResultModel result)
        {
            if (string.IsNullOrEmpty(result.SymbolicId) &&
                kSymbolicIds.TryGetValue(result.StatusCode & 0xFFFF0000, out var symbolicId))
            {
                result.SymbolicId = symbolicId;
            }
            result.Inner?.WithSymbolicId();
            return result;
        }

        private static IReadOnlyDictionary<uint, string> CreateSymbolicIds()
        {
            return typeof(StatusCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => (field.Name, Value: field.GetValue(null)))
                .Select(field => field.Value switch
                {
                    uint code => (Code: code & 0xFFFF0000, field.Name),
                    StatusCode code => (Code: code.CodeBits,
                        Name: code.SymbolicId ?? field.Name),
                    _ => (Code: uint.MaxValue, Name: string.Empty)
                })
                .Where(entry => entry.Code != uint.MaxValue)
                .GroupBy(entry => entry.Code)
                .ToDictionary(group => group.Key, group => group.First().Name);
        }

        private static readonly IReadOnlyDictionary<uint, string> kSymbolicIds =
            CreateSymbolicIds();
    }
}
