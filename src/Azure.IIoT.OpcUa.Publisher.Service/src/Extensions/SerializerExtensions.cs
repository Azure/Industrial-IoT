// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Furly.Extensions.Serializers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Serializer extensions
    /// </summary>
    internal static class SerializerExtensions
    {
        /// <summary>
        /// Helper to get values from token dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this IReadOnlyDictionary<string, VariantValue>? dict,
            string key, T? defaultValue)
        {
            if (dict != null && dict.TryGetValue(key, out var token) && token != null)
            {
                try
                {
                    return token.ConvertTo<T>();
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Helper to get values from token dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this IReadOnlyDictionary<string, VariantValue>? dict,
            string key, T? defaultValue) where T : struct
        {
            if (dict != null && dict.TryGetValue(key, out var token) && token != null)
            {
                try
                {
                    // Handle enumerations serialized as string
                    if (typeof(T).IsEnum &&
                        token.IsString &&
                        Enum.TryParse<T>((string?)token, out var result))
                    {
                        return result;
                    }
                    return token.ConvertTo<T>();
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this VariantValue t, string key)
        {
            return GetValueOrDefault(t, key, () => default(T));
        }

        /// <summary>
        /// Helper to get values from object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this VariantValue t,
            string key, Func<T> defaultValue)
        {
            if (t.IsObject &&
                t.TryGetProperty(key, out var value) &&
                value is not null)
            {
                try
                {
                    return value.ConvertTo<T>();
                }
                catch
                {
                    return defaultValue();
                }
            }
            return defaultValue();
        }
    }
}
