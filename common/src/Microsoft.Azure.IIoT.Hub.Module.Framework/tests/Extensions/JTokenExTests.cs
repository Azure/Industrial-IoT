// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework {
    using Xunit;
    using Newtonsoft.Json.Linq;

    public class JTokenExTests {

        [Fact]
        public void WhenNullPassedNullIsReturned() {

            JToken j1 = "test";

            var result = j1.Apply(null);

            Assert.True(JValue.CreateNull().Equals(result));
        }

    }
}
