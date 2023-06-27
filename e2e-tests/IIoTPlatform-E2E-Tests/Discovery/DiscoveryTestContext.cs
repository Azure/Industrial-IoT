// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Discovery
{
    using IIoTPlatformE2ETests.TestExtensions;
    using System;
    using System.Threading.Tasks;

    public class DiscoveryTestContext : IIoTPlatformTestContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryTestContext"/> class.
        /// Used for preparation executed once before any tests of the collection are started.
        /// </summary>
        public DiscoveryTestContext()
        {
        }

        /// <summary>
        /// Disposes resources.
        /// Used for cleanup executed once after all tests of the collection were executed.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            // OutputHelper cannot be used outside of test calls, we get rid of it before a helper method would use it
            OutputHelper = null;

            base.Dispose(true);
        }
    }
}
