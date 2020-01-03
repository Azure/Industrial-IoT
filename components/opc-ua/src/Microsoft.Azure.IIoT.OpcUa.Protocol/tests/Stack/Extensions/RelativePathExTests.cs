// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Xunit;

    public class RelativePathExTests {

        [Fact]
        public void EncodeDecodePath1() {

            var path = new string[] {
                "<!#http://contoso.com/ua#i=44>Test",
                "<!HasChild>Test",
                "<#HasChild>Test",
                "<!#HasProperty>Test",
                "<HasComponent>Test",
                "/foo",
                ".bar",
                "/!#flah",
                "!#flah",
                "xxxx",
            };

            var context = new ServiceMessageContext();
            var relative = path.ToRelativePath(context);
            var result = relative.AsString(context);

            Assert.Equal(path, result);
        }

        [Fact]
        public void EncodeDecodePath2() {

            var path = new string[] {
                "<!HasChild>Test",
                "<#http://opcfoundation.org/ua#i_33>Test",
                "<#!HasProperty>Test",
                "<#!http://contoso.com/ua#i_44>Test",
                "<http://opcfoundation.org/ua#i_33>Test",
                "#foo",
                "!.bar",
                "!#/flah",
                "!/#flah",
                "!xxxx",
            };

            var context = new ServiceMessageContext();
            var relative = path.ToRelativePath(context);
            var expected = relative.AsString(context);
            relative = expected.ToRelativePath(context);
            var result = relative.AsString(context);

            Assert.Equal(expected, result);
        }
    }
}
