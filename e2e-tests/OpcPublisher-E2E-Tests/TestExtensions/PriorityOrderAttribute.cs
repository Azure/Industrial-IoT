// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestExtensions {
    using System;

    /// <summary>
    /// Attribute to define ordering between xUnit tests
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PriorityOrderAttribute : Attribute {

        /// <summary>
        /// Constructor to create instance of priority order attribute
        /// </summary>
        /// <param name="order"></param>
        public PriorityOrderAttribute(uint order) {
            Order = order;
        }

        /// <summary>
        /// The order in which the test should be executed
        /// </summary>
        public uint Order { get; }

    }
}
