// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services.Models
{
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Helper utility extensions of several collections and types
    /// to support simple encoding and decoding where the type
    /// itself cannot be stored as-is in the IoT Hub twin record.
    /// This includes lists and byte arrays that are longer than
    /// the max field size and more.
    /// </summary>
    public static class IoTHubExtensions
    {
        /// <summary>
        /// Query hub for device twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<DeviceTwinModel>> QueryAllDeviceTwinsAsync(
            this IIoTHubTwinServices service, string query, CancellationToken ct = default)
        {
            var result = new List<DeviceTwinModel>();
            string? continuation = null;
            do
            {
                var response = await service.QueryDeviceTwinsAsync(query, continuation,
                    null, ct).ConfigureAwait(false);
                result.AddRange(response.Items);
                continuation = response.ContinuationToken;
            }
            while (continuation != null);
            return result;
        }

        /// <summary>
        /// Check whether twin is connected
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static bool? IsConnected(this DeviceTwinModel twin)
        {
            if (twin.ConnectionState == null)
            {
                return null;
            }
            return StringComparer.OrdinalIgnoreCase.Equals(twin.ConnectionState, "Connected");
        }

        /// <summary>
        /// Check whether twin is disabled
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static bool? IsDisabled(this DeviceTwinModel twin)
        {
            if (twin.Status == null)
            {
                return null;
            }
            return StringComparer.OrdinalIgnoreCase.Equals(twin.Status, "disabled");
        }

        /// <summary>
        /// Consolidated
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, VariantValue> GetConsolidatedProperties(
            this DeviceTwinModel model)
        {
            var desired = model.Desired;
            var reported = model.Reported;
            if (reported == null || desired == null)
            {
                return (reported ?? desired) ??
                    new Dictionary<string, VariantValue>();
            }

            var properties = new Dictionary<string, VariantValue>(desired);

            // Merge with reported
            foreach (var prop in reported)
            {
                if (properties.TryGetValue(prop.Key, out var existing))
                {
                    if (existing.IsNull() || prop.Value.IsNull())
                    {
                        if (existing.IsNull() && prop.Value.IsNull())
                        {
                            continue;
                        }
                    }
                    else if (VariantValue.DeepEquals(existing, prop.Value))
                    {
                        continue;
                    }
                    properties[prop.Key] = prop.Value;
                }
                else
                {
                    properties.Add(prop.Key, prop.Value);
                }
            }
            return properties;
        }
        /// <summary>
        /// Convert list to dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, T>? EncodeAsDictionary<T>(
            this IReadOnlyList<T>? list)
        {
            return EncodeAsDictionary(list, t => t);
        }

        /// <summary>
        /// Convert list to dictionary
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="list"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, TValue>? EncodeAsDictionary<TKey, TValue>(
            this IReadOnlyList<TKey>? list, Func<TKey, TValue> converter)
        {
            if (list == null)
            {
                return null;
            }
            var result = new Dictionary<string, TValue>();
            for (var i = 0; i < list.Count; i++)
            {
                result.Add(i.ToString(CultureInfo.InvariantCulture), converter(list[i]));
            }
            return result;
        }

        /// <summary>
        /// Convert dictionary to list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static IReadOnlyList<T>? DecodeAsList<T>(this IReadOnlyDictionary<string, T>? dictionary)
        {
            return DecodeAsList(dictionary, t => t);
        }

        /// <summary>
        /// Convert dictionary to list
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static IReadOnlyList<TKey>? DecodeAsList<TKey, TValue>(
            this IReadOnlyDictionary<string, TValue>? dictionary,
            Func<TValue, TKey> converter)
        {
            if (dictionary == null)
            {
                return null;
            }
            var result = Enumerable.Repeat(default(TKey)!, dictionary.Count).ToList();
            foreach (var kv in dictionary)
            {
                result[int.Parse(kv.Key, CultureInfo.InvariantCulture)] = converter(kv.Value);
            }
            return result;
        }

        /// <summary>
        /// Convert dictionary to string set
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static IReadOnlySet<string>? DecodeAsSet(
            this IReadOnlyDictionary<string, bool>? dictionary)
        {
            if (dictionary == null)
            {
                return null;
            }
            return new HashSet<string>(dictionary.Select(kv => kv.Key));
        }

        /// <summary>
        /// Convert string set to queryable dictionary
        /// </summary>
        /// <param name="set"></param>
        /// <param name="upperCase"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, bool>? EncodeAsDictionary(
            this IReadOnlySet<string>? set, bool? upperCase = null)
        {
            if (set == null)
            {
                return null;
            }
            var result = new Dictionary<string, bool>();
            foreach (var s in set)
            {
                var add = SanitizePropertyName(s);
                if (upperCase != null)
                {
#pragma warning disable CA1308 // Normalize strings to uppercase
                    add = (bool)upperCase ? add.ToUpperInvariant() : add.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
                }
                result.Add(add, true);
            }
            return result;
        }

        /// <summary>
        /// Replace whitespace in a property name
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SanitizePropertyName(this string value)
        {
            var chars = new char[value.Length];
            for (var i = 0; i < value.Length; i++)
            {
                chars[i] = !char.IsLetterOrDigit(value[i]) ? '_' : value[i];
            }
            return new string(chars);
        }
    }
}
