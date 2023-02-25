// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System
{
    using Xunit;

    public class AssemblyExTests
    {
        [Fact]
        public void GetReleaseVersionTest()
        {
            var v = GetType().Assembly.GetReleaseVersion().ToString();
            Assert.NotEmpty(v);
        }
    }
}
