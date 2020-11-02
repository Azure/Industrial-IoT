// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests
{
    using System;
    using Xunit;
    using System.Collections;
    using Xunit.Abstractions;

    public class DummyTests
    {
        private readonly ITestOutputHelper _output;

        public DummyTests(ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void TestDummy()
        {
            foreach (DictionaryEntry kvp in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)) {
                _output.WriteLine($"key: {kvp.Key} value:{kvp.Value}");
                Assert.True(true);
            }
        }
    }
}
