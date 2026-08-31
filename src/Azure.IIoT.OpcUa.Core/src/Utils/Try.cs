// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Utils
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Helper class to wrap operations in try catch
    /// </summary>
#pragma warning disable CA1716 // Identifiers should not match keywords
    public static class Try
#pragma warning restore CA1716 // Identifiers should not match keywords
    {
        /// <summary>
        /// Try operation
        /// </summary>
        /// <param name="action"></param>
        public static bool Op(Action action)
        {
            try
            {
                action.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Try operation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public static T? Op<T>(Func<T> action)
        {
            try
            {
                return action.Invoke();
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Try operation
        /// </summary>
        /// <param name="action"></param>
        public static Task<bool> Async(Func<Task> action)
        {
            return action.Invoke()
                .ContinueWith(t => t.IsCompletedSuccessfully,
                    default, TaskContinuationOptions.None, TaskScheduler.Current);
        }

        /// <summary>
        /// Try operation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public static Task<T?> Async<T>(Func<Task<T>> action)
        {
            return action.Invoke()
                .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : default,
                    default, TaskContinuationOptions.None, TaskScheduler.Current);
        }
    }
}
