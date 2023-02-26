// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub
{
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Helper utility extensions of several collections and types
    /// to support simple encoding and decoding where the type
    /// itself cannot be stored as-is in the IoT Hub twin record.
    /// This includes lists and byte arrays that are longer than
    /// the max field size and more.
    /// </summary>
    public static class DeviceTwinEncodingEx
    {
        /// <summary>
        /// Convert list to dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, T> EncodeAsDictionary<T>(
            this IReadOnlyList<T> list)
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
        public static IReadOnlyDictionary<string, TValue> EncodeAsDictionary<TKey, TValue>(
            this IReadOnlyList<TKey> list, Func<TKey, TValue> converter)
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
        public static IReadOnlyList<T> DecodeAsList<T>(this IReadOnlyDictionary<string, T> dictionary)
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
        public static IReadOnlyList<TKey> DecodeAsList<TKey, TValue>(
            this IReadOnlyDictionary<string, TValue> dictionary,
            Func<TValue, TKey> converter)
        {
            if (dictionary == null)
            {
                return null;
            }
            var result = Enumerable.Repeat(default(TKey), dictionary.Count).ToList();
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
        public static IReadOnlySet<string> DecodeAsSet(
            this IReadOnlyDictionary<string, bool> dictionary)
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
        public static IReadOnlyDictionary<string, bool> EncodeAsDictionary(
            this IReadOnlySet<string> set, bool? upperCase = null)
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
                    add = (bool)upperCase ? add.ToUpperInvariant() : add.ToLowerInvariant();
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
        public static string SanitizePropertyName(string value)
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
