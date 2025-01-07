// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

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
        /// <typeparam name="TResult"></typeparam>
        /// <param name="state"></param>
        /// <param name="convert"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TResult? GetValueOrDefaultEx<T, TResult>(this PropertyState<T> state,
            Func<T?, TResult?> convert, T? defaultValue = default) where T : struct
        {
            var result = GetValueOrDefaultEx(state, defaultValue);
            return convert(result);
        }

        /// <summary>
        /// Return value or update
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="state"></param>
        /// <param name="convert"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TResult? GetValueOrDefaultEx<TValue, TResult>(this PropertyState<TValue> state,
            Func<TValue?, TResult?> convert, TValue? defaultValue = null) where TValue : class
        {
            var result = GetValueOrDefaultEx(state, defaultValue);
            return convert(result);
        }

        /// <summary>
        /// Return value or default
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="state"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T? GetValueOrDefaultEx<T>(this PropertyState<T> state,
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
        public static T? GetValueOrDefaultEx<T>(this PropertyState<T> state,
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
