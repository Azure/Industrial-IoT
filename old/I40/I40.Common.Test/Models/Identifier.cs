// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using AutoFixture;
    using I40.Common.Identifiers;
    using System;
    using System.Linq;
    using Xunit;

    public class IdentiferTests {

        [Fact]
        public void TestIrdiCast() {
            var irdi = CreateIrdi();

            Identification id = irdi;

            Assert.Equal(irdi, id);
            Assert.Equal(irdi.ToString(), id.Id);
            Assert.Equal(IdentificationKind.Irdi, id.Kind);
        }

        [Fact]
        public void TestUriCast() {
            var uri = CreateUri();

            Identification id = uri;

            Assert.Equal(uri, id);
            Assert.Equal(uri.ToString(), id.Id);
            Assert.Equal(IdentificationKind.Uri, id.Kind);
        }

        [Fact]
        public void TestEqualIsEqual1() {
            Identification i1 = CreateIrdi();
            var i2 = i1;

            Assert.Equal(i1, i2);
            Assert.Equal(i1.GetHashCode(), i2.GetHashCode());
            Assert.True(i1 == i2);
            Assert.False(i1 != i2);
        }

        [Fact]
        public void TestEqualIsEqual2() {
            Identification i1 = CreateUri();
            var i2 = i1;

            Assert.Equal(i1, i2);
            Assert.Equal(i1.GetHashCode(), i2.GetHashCode());
            Assert.True(i1 == i2);
            Assert.False(i1 != i2);
        }

        [Fact]
        public void TestEqualIsNotEqual() {
            Identification i1 = CreateIrdi();
            Identification i2 = CreateUri();

            Assert.NotEqual(i1, i2);
            Assert.False(i1.Equals(null));
            Assert.NotEqual(i1.GetHashCode(), i2.GetHashCode());
            Assert.True(i1 != i2);
            Assert.False(i1 == i2);
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static Uri CreateUri() {
            var fix = new Fixture();
            return fix.Create<Uri>();
        }

        /// <summary>
        /// Create registration
        /// </summary>
        /// <returns></returns>
        private static Irdi CreateIrdi() {
            var fix = new Fixture();
            var irdi = new Irdi(
                new Rai(fix.Create<ushort>(), CreateString(fix, 4), CreateString(fix, 4),
                    byte.Parse(fix.Create<byte>().ToString().Substring(0, 1)),
                    CreateString(fix, 4)),
                new Di(fix.Create<Csi>(), CreateString(fix, 6)),
                fix.Create<byte>().ToString());
            return Irdi.Parse(irdi.ToString());
        }

        private static string CreateString(Fixture fixture, int length) =>
            length == 0 ? null : new string(
                fixture.CreateMany<string>(length).Select(s => s[0]).ToArray());
    }
}
