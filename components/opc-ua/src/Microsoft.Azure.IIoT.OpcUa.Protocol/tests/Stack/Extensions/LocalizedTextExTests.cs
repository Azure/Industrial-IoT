﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Xunit;

    public class LocalizedTextExTests {

        [Fact]
        public void DecodeLocalizedTextWithLocale() {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText("en-US", "text");
            var result = "text@en-US".ToLocalizedText();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DecodeLocalizedTextWithLocale2() {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText("en-US", "text@");
            var result = "text@@en-US".ToLocalizedText();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DecodeLocalizedTextWithoutLocale() {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText("text");
            var result = "text".ToLocalizedText();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DecodeLocalizedTextWithoutLocale1() {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText("text");
            var result = "text@".ToLocalizedText();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DecodeLocalizedTextWithTwoAtAndLocale() {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText("en-US", "text@contoso.org");
            var result = "text@contoso.org@en-US".ToLocalizedText();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DecodeLocalizedTextWithTwoAtAndWithoutLocale() {
            var context = new ServiceMessageContext();
            var expected = new LocalizedText("text@contoso.org");
            var result = "text@contoso.org@".ToLocalizedText();
            Assert.Equal(expected, result);
        }
    }
}
