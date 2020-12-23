// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System {
    using Xunit;
    using System.Linq;

    public class AssemblyExTests {

        [Fact]
        public void GetVersionInfoObjectTests() {
            var o = GetType().Assembly.GetVersionInfoObject();
            Assert.NotNull(o);
            var s = o.ToString();
            Assert.True(o.Properties().Any());
        }

        [Fact]
        public void GetFileVersion() {
            var o = GetType().Assembly.GetVersionInfoObject();
            Assert.NotNull(o);
            var v = GetType().Assembly.GetReleaseVersion().ToString();
            Assert.Equal(v, (string)o.GetValue("AssemblyFileVersion"));
        }
    }
}
