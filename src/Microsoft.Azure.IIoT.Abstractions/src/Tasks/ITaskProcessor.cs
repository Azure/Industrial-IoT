// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks {
    using System;
    using System.Threading.Tasks;

    public interface ITaskProcessor : IDisposable {

        /// <summary>
        /// Try enqueue task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="checkpoint"></param>
        /// <returns></returns>
        bool TrySchedule(Func<Task> task, Func<Task> checkpoint);
    }
}
