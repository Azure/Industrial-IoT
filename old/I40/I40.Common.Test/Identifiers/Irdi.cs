// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Identifiers {
    using AutoFixture;
    using System;
    using System.Linq;
    using Xunit;

    public class IrdiTests {

        [Fact]
        public void TestEqualIsEqual1() {
            var r1 = CreateIrdi();
            var r2 = r1;

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqual2() {
            var r1 = CreateIrdi();
            var r2 = Irdi.Parse(r1.ToString());

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsEqual3() {
            var r0 = CreateIrdi();
            var r1 = Irdi.Parse(r0.ToString(), true);
            var r2 = Irdi.Parse(r1.ToString(), true);

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Theory]
        [InlineData(0, false, 0)]
        [InlineData(0, false, 4)]
        [InlineData(0, true, 0)]
        [InlineData(0, true, 4)]
        [InlineData(6, true, 0)]
        [InlineData(6, false, 0)]
        [InlineData(6, false, 4)]
        public void TestEqualIsEqual1WithOptionals1(int opi, bool opis,
            int ai) {
            var r1 = CreateIrdi(4, opi, opis, ai, 6);
            var r2 = r1;

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Theory]
        [InlineData(0, false, 0)]
        [InlineData(0, false, 4)]
        [InlineData(0, true, 0)]
        [InlineData(0, true, 4)]
        [InlineData(6, true, 0)]
        [InlineData(6, false, 0)]
        [InlineData(6, false, 4)]
        public void TestEqualIsEqual1WithOptionals2(int opi, bool opis,
            int ai) {
            var r1 = CreateIrdi(4, opi, opis, ai, 6);
            var r2 = Irdi.Parse(r1.ToString());

            Assert.Equal(r1, r2);
            Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 == r2);
            Assert.False(r1 != r2);
        }

        [Fact]
        public void TestEqualIsNotEqual() {
            var r1 = CreateIrdi();
            var r2 = CreateIrdi();

            Assert.NotEqual(r1, r2);
            Assert.False(r1.Equals(null));
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Fact]
        public void TestEqualIsNotEqualWithDifferentVersions() {
            var r0 = CreateIrdi();
            var r1 = Irdi.Parse(r0.ToString(), false);
            var r2 = Irdi.Parse(r1.ToString(), true);

            Assert.NotEqual(r1, r2);
            Assert.False(r1.Equals(null));
            Assert.NotEqual(r1.GetHashCode(), r2.GetHashCode());
            Assert.True(r1 != r2);
            Assert.False(r1 == r2);
        }

        [Theory]
        [InlineData(80, 4)]
        [InlineData(4, 80)]
        public void TestThrowsWithBadFormatForLength(int oi, int opi) {
            Assert.Throws<FormatException>(() => CreateIrdi(
                oi, opi, true, 4, 5));
        }

        [Fact]
        public void TestThrowsWithBadFormatWhenNoParts() {
            var fix = new Fixture();
            Assert.Throws<FormatException>(() => Irdi.Parse(
                CreateString(fix, 40)));
        }

        [Fact]
        public void TestThrowsWithBadFormatWhenTooManyParts() {
            var fix = new Fixture();
            Assert.Throws<FormatException>(() => Irdi.Parse(
                CreateString(fix, 40)));
        }

        [Fact]
        public void TestThrowsWithStringToParseBeingNull() {
            var fix = new Fixture();
            Assert.Throws<ArgumentNullException>(() => Irdi.Parse(null));
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static Irdi CreateIrdi(int oi = 4, int opi = 4, bool opis = true, 
            int ai = 4, int ic = 6) {
            var fix = new Fixture();
            var irdi = new Irdi(
                new Rai(fix.Create<ushort>(),
                    CreateString(fix, oi),
                    CreateString(fix, opi),
                    !opis ? (byte?)null : 
                        byte.Parse(fix.Create<byte>().ToString().Substring(0, 1)),
                    CreateString(fix, ai)),
                new Di(fix.Create<Csi>(), CreateString(fix, ic)),
                fix.Create<byte>().ToString());
            return Irdi.Parse(irdi.ToString());
        }

        private static string CreateString(Fixture fixture, int length) =>
            length == 0 ? null : new string(
                fixture.CreateMany<string>(length).Select(s => s[0]).ToArray());
    }
}
