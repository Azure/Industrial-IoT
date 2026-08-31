// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using Xunit;

    public sealed class AuthenticationMethodModelExTests
    {
        [Fact]
        public void IsSameAs_ListSameReference_ReturnsTrue()
        {
            IReadOnlyList<AuthenticationMethodModel> methods =
            [
                CreateMethod("anonymous")
            ];

            Assert.True(methods.IsSameAs(methods));
        }

        [Fact]
        public void IsSameAs_ListOneNull_ReturnsFalse()
        {
            IReadOnlyList<AuthenticationMethodModel>? methods =
            [
                CreateMethod("anonymous")
            ];

            Assert.False(methods.IsSameAs(null));
            Assert.False(((IReadOnlyList<AuthenticationMethodModel>?)null).IsSameAs(methods));
        }

        [Fact]
        public void IsSameAs_ListDifferentCount_ReturnsFalse()
        {
            IReadOnlyList<AuthenticationMethodModel> left =
            [
                CreateMethod("anonymous")
            ];
            IReadOnlyList<AuthenticationMethodModel> right =
            [
                CreateMethod("anonymous"),
                CreateMethod("username")
            ];

            Assert.False(left.IsSameAs(right));
        }

        [Fact]
        public void IsSameAs_ListSameItemsDifferentOrder_ReturnsTrue()
        {
            IReadOnlyList<AuthenticationMethodModel> left =
            [
                CreateMethod("anonymous"),
                CreateMethod("username")
            ];
            IReadOnlyList<AuthenticationMethodModel> right =
            [
                CreateMethod("username"),
                CreateMethod("anonymous")
            ];

            Assert.True(left.IsSameAs(right));
        }

        [Fact]
        public void IsSameAs_ListMissingMatchingItem_ReturnsFalse()
        {
            IReadOnlyList<AuthenticationMethodModel> left =
            [
                CreateMethod("anonymous")
            ];
            IReadOnlyList<AuthenticationMethodModel> right =
            [
                CreateMethod("username")
            ];

            Assert.False(left.IsSameAs(right));
        }

        [Fact]
        public void IsSameAs_MethodSameReference_ReturnsTrue()
        {
            var method = CreateMethod("anonymous");

            Assert.True(method.IsSameAs(method));
        }

        [Fact]
        public void IsSameAs_MethodOneNull_ReturnsFalse()
        {
            var method = CreateMethod("anonymous");

            Assert.False(method.IsSameAs(null));
            Assert.False(((AuthenticationMethodModel?)null).IsSameAs(method));
        }

        [Fact]
        public void IsSameAs_MethodSameValuesAndJsonConfiguration_ReturnsTrue()
        {
            var left = CreateMethod("username") with
            {
                Configuration = JsonNode.Parse("""{"name":"user"}""")
            };
            var right = CreateMethod("username") with
            {
                Configuration = JsonNode.Parse("""{"name":"user"}""")
            };

            Assert.True(left.IsSameAs(right));
        }

        [Fact]
        public void IsSameAs_MethodDifferentConfiguration_ReturnsFalse()
        {
            var left = CreateMethod("username") with
            {
                Configuration = JsonNode.Parse("""{"name":"user"}""")
            };
            var right = CreateMethod("username") with
            {
                Configuration = JsonNode.Parse("""{"name":"other"}""")
            };

            Assert.False(left.IsSameAs(right));
        }

        [Fact]
        public void IsSameAs_MethodDifferentId_ReturnsFalse()
        {
            var left = CreateMethod("anonymous");
            var right = CreateMethod("username");

            Assert.False(left.IsSameAs(right));
        }

        [Fact]
        public void IsSameAs_MethodDifferentSecurityPolicy_ReturnsFalse()
        {
            var left = CreateMethod("anonymous") with
            {
                SecurityPolicy = "policy1"
            };
            var right = CreateMethod("anonymous") with
            {
                SecurityPolicy = "policy2"
            };

            Assert.False(left.IsSameAs(right));
        }

        [Fact]
        public void IsSameAs_MethodDifferentCredentialType_ReturnsFalse()
        {
            var left = CreateMethod("anonymous") with
            {
                CredentialType = CredentialType.None
            };
            var right = CreateMethod("anonymous") with
            {
                CredentialType = CredentialType.UserName
            };

            Assert.False(left.IsSameAs(right));
        }

        private static AuthenticationMethodModel CreateMethod(string id)
        {
            return new AuthenticationMethodModel
            {
                Id = id,
                CredentialType = id == "anonymous" ?
                    CredentialType.None :
                    CredentialType.UserName,
                SecurityPolicy = "policy"
            };
        }
    }
}
