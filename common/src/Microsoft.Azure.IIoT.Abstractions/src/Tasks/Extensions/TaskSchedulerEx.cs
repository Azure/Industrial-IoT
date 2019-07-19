// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Task scheduler extensions
    /// </summary>
    public static class TaskSchedulerEx {

        /// <summary>
        /// Schedule func on scheduler
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task Run(this ITaskScheduler scheduler, Func<Task> func) {
            return scheduler.Factory.StartNew(func).Unwrap();
        }

        /// <summary>
        /// Schedule func on scheduler
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scheduler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(this ITaskScheduler scheduler, Func<Task<T>> func) {
            return scheduler.Factory.StartNew(func).Unwrap();
        }

        /// <summary>
        /// Schedule func on scheduler
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task Run(this ITaskScheduler scheduler, Action func) {
            return scheduler.Factory.StartNew(func);
        }

        /// <summary>
        /// Schedule func on scheduler
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scheduler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(this ITaskScheduler scheduler, Func<T> func) {
            return scheduler.Factory.StartNew(func);
        }
    }
}
