// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework {
    using System;
    using System.Threading.Tasks;

    public static class TaskSchedulerEx {

        /// <summary>
        /// Schedule func on scheduler
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task Run(this ITaskScheduler scheduler, Func<Task> func) =>
            scheduler.Factory.StartNew(func).Unwrap();

        /// <summary>
        /// Schedule func on scheduler
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scheduler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(this ITaskScheduler scheduler, Func<Task<T>> func) =>
            scheduler.Factory.StartNew(func).Unwrap();

        /// <summary>
        /// Schedule func on scheduler
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task Run(this ITaskScheduler scheduler, Action func) =>
            scheduler.Factory.StartNew(func);

        /// <summary>
        /// Schedule func on scheduler
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scheduler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(this ITaskScheduler scheduler, Func<T> func) =>
            scheduler.Factory.StartNew(func);
    }
}
