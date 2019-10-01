// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic.Services {
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Test publisher
    /// </summary>
    public interface IPublisher : IHost {

        /// <summary>
        /// Interval setting
        /// </summary>
        TimeSpan Interval { get; set; }
    }
}
