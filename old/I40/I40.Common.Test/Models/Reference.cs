// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using AutoFixture;
    using System;
    using System.Linq;
    using Xunit;

    public class ReferenceTests {

        [Fact]
        public void TestEqualIsEqual1() {
            var r1 = CreateReference();
            var r2 = r1;

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqual2() {
            var r1 = CreateReference(4);
            var r2 = r1;

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqual3() {
            var r1 = CreateReference();
            var r2 = r1.AsString().AsReference();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqual4() {
            var r1 = CreateReference(4);
            var r2 = r1.AsString().AsReference();

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqual() {
            var r1 = CreateReference();
            var r2 = CreateReference();

            Assert.NotEqual(r1, r2);
            Assert.False(r1.Equals(null));
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestCastToIdentifier() {
            var r = CreateReference();
            var i = (Identification)r;

            Assert.NotNull(i);
            Assert.Equal(r.Target, i);
        }

        [Fact]
        public void TestCastToIdentifierThrowsWithCast() {
            var r = CreateReference(4);
            Assert.Throws<InvalidCastException>(() => {
                var i = (Identification)r;
            });
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static Reference CreateReference(int p = 0) {
            var fix = new Fixture();
            return new Reference {
                Target = fix.Create<Uri>(),
                SubPath = p == 0 ? null : fix.CreateMany<string>(p).ToList()
            };
        }
    }
}
