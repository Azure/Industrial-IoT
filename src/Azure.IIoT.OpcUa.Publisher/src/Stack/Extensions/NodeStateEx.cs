// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable
namespace Opc.Ua.Extensions
{
    using Opc.Ua;
    using System;

    /// <summary>
    /// Node state extensions
    /// </summary>
    public static class NodeStateEx
    {
        /// <summary>
        /// Return value or update
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="state"></param>
        /// <param name="convert"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static R? GetValueOrDefault<T, R>(this PropertyState<T> state,
            Func<T?, R?> convert, T? defaultValue = default) where T : struct
        {
            var result = GetValueOrDefault(state, defaultValue);
            return convert(result);
        }

        /// <summary>
        /// Return value or update
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="state"></param>
        /// <param name="convert"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static R? GetValueOrDefault<T, R>(this PropertyState<T> state,
            Func<T?, R?> convert, T? defaultValue = null) where T : class
        {
            var result = GetValueOrDefault(state, defaultValue);
            return convert(result);
        }

        /// <summary>
        /// Return value or default
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="state"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this PropertyState<T> state,
            T? defaultValue = default) where T : struct
        {
            if (!StatusCode.IsGood(state.StatusCode))
            {
                return defaultValue;
            }
            return state.Value;
        }

        /// <summary>
        /// Return value or default
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="state"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? GetValueOrDefault<T>(this PropertyState<T> state,
            T? defaultValue = null) where T : class
        {
            if (!StatusCode.IsGood(state.StatusCode))
            {
                return defaultValue;
            }
            return state.Value;
        }
    }
}
