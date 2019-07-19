// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {
    using Xunit;

    /// <summary>
    /// Simple bitmap on top of ulong list
    /// </summary>
    public class BitmapTests {

        [Fact]
        public void Test64Bitmap() {
            var bmp = new Bitmap();

            Assert.Equal(0u, bmp.Allocate());
            Assert.Equal(1u, bmp.Allocate());
            Assert.Equal(2u, bmp.Allocate());
            Assert.Equal(3u, bmp.Allocate());
            Assert.True(bmp.Free(2u));
            Assert.Equal(2u, bmp.Allocate());
            Assert.Equal(4u, bmp.Allocate());
            Assert.True(bmp.Free(4u));
            Assert.Equal(4u, bmp.Allocate());
            Assert.True(bmp.Free(1u));
            Assert.True(bmp.Free(2u));
            Assert.True(bmp.Free(3u));
            Assert.True(bmp.Free(4u));
            Assert.Equal(1u, bmp.Allocate());
            Assert.True(bmp.Free(1u));
            Assert.True(bmp.Free(0u));
            Assert.Equal(0u, bmp.Allocate());
        }

        [Fact]
        public void TestLargeBitmap() {
            var bmp = new Bitmap();
            for (var i = 0u; i < 10000u; i++) {
                Assert.Equal(i, bmp.Allocate());
            }
            Assert.Equal(10000u, bmp.Allocate());
            Assert.True(bmp.Free(2u));
            Assert.Equal(2u, bmp.Allocate());
            for (var i = 0u; i < 1000u; i++) {
                Assert.True(bmp.Free(i));
            }
            Assert.Equal(0u, bmp.Allocate());
            Assert.Equal(1u, bmp.Allocate());
            Assert.True(bmp.Free(1u));
            Assert.True(bmp.Free(0u));
            Assert.False(bmp.Free(0u));
            for (var i = 0u; i < 1000u; i++) {
                Assert.Equal(i, bmp.Allocate());
            }
            Assert.Equal(10001u, bmp.Allocate());
            Assert.Equal(10002u, bmp.Allocate());
            Assert.True(bmp.Free(656u));
            Assert.True(bmp.Free(256u));
            Assert.Equal(256u, bmp.Allocate());
            Assert.Equal(656u, bmp.Allocate());
        }


        //   [Fact]
        //   public void TestVeryLargeBitmap() {
        //       var bmp = new Bitmap();
        //       for (var i = 0u; i < 10000000u; i++) {
        //           Assert.Equal(i, bmp.Get());
        //       }
        //       Assert.Equal(10000000u, bmp.Get());
        //       bmp.Set(2u);
        //       Assert.Equal(2u, bmp.Get());
        //       for (var i = 0u; i <= 10000000u; i++) {
        //           bmp.Set(i);
        //       }
        //       Assert.Equal(0u, bmp.Get());
        //   }


        [Fact]
        public void TestOutOfBoundsBitmap() {
            var bmp = new Bitmap();
            Assert.Equal(0u, bmp.Allocate());
            Assert.Equal(1u, bmp.Allocate());
            Assert.False(bmp.Free(5u));
            Assert.False(bmp.Free(1000u));
            Assert.True(bmp.Free(0u));
            Assert.True(bmp.Free(1u));
            for (var i = 0u; i < 64u; i++) {
                Assert.Equal(i, bmp.Allocate());
            }
            Assert.Equal(64u, bmp.Allocate());
            Assert.True(bmp.Free(64u));
            for (var i = 0u; i < 64u; i++) {
                Assert.Equal(i + 64u, bmp.Allocate());
            }
            Assert.Equal(128u, bmp.Allocate());
        }
    }
}
