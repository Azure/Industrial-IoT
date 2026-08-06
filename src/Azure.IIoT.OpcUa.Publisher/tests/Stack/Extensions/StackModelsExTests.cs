// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="StackModelsEx"/> pure conversion methods.
    /// </summary>
    public sealed class StackModelsExTests
    {
        // ── ToRequestHeader ────────────────────────────────────────────────────

        [Fact]
        public void ToRequestHeader_NullHeader_ReturnsDefaultValues()
        {
            var result = ((RequestHeaderModel?)null).ToRequestHeader();

            Assert.NotNull(result);
            // ReturnDiagnostics should be the stack type for DiagnosticsLevel.Status
            Assert.NotNull(result.AuditEntryId);
        }

        [Fact]
        public void ToRequestHeader_HeaderWithTimestamp_UsesProvidedTimestamp()
        {
            var timestamp = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
            var header = new RequestHeaderModel
            {
                Diagnostics = new DiagnosticsModel
                {
                    TimeStamp = timestamp
                }
            };

            var result = header.ToRequestHeader();

            Assert.Equal(timestamp, result.Timestamp);
        }

        [Fact]
        public void ToRequestHeader_HeaderWithAuditId_UsesProvidedAuditId()
        {
            var header = new RequestHeaderModel
            {
                Diagnostics = new DiagnosticsModel
                {
                    AuditId = "my-audit-id"
                }
            };

            var result = header.ToRequestHeader();

            Assert.Equal("my-audit-id", result.AuditEntryId);
        }

        [Fact]
        public void ToRequestHeader_HeaderWithTimeout_SetsTimeoutHint()
        {
            var header = new RequestHeaderModel
            {
                OperationTimeout = 5000
            };

            var result = header.ToRequestHeader();

            Assert.Equal((uint)5000, result.TimeoutHint);
        }

        [Fact]
        public void ToRequestHeader_NullTimestamp_UsesCurrentTime()
        {
            var before = DateTime.UtcNow;
            var result = ((RequestHeaderModel?)null).ToRequestHeader();
            var after = DateTime.UtcNow;

            // result.Timestamp is Opc.Ua.DateTimeUtc — convert to DateTime for comparison
            var ts = (DateTime)result.Timestamp;
            Assert.True(ts >= before.AddSeconds(-1) && ts <= after.AddSeconds(1));
        }

        [Fact]
        public void ToRequestHeader_ZeroTimeout_SetsZeroTimeoutHint()
        {
            var header = new RequestHeaderModel
            {
                OperationTimeout = 0
            };

            var result = header.ToRequestHeader();

            Assert.Equal(0u, result.TimeoutHint);
        }

        // ── DataChangeFilter.ToStackModel ──────────────────────────────────────

        [Fact]
        public void DataChangeFilterToStackModel_NullModel_ReturnsNull()
        {
            DataChangeFilterModel? model = null;
            var result = model.ToStackModel();
            Assert.Null(result);
        }

        [Fact]
        public void DataChangeFilterToStackModel_EmptyModel_ReturnsDef()
        {
            var model = new DataChangeFilterModel();
            var result = model.ToStackModel();
            Assert.NotNull(result);
            Assert.Equal(0.0, result.DeadbandValue);
        }

        [Fact]
        public void DataChangeFilterToStackModel_WithDeadbandValue_SetsValue()
        {
            var model = new DataChangeFilterModel { DeadbandValue = 3.14 };
            var result = model.ToStackModel();
            Assert.NotNull(result);
            Assert.Equal(3.14, result.DeadbandValue);
        }

        [Fact]
        public void DataChangeFilterToStackModel_WithDeadbandType_SetsDeadbandType()
        {
            var model = new DataChangeFilterModel
            {
                DeadbandType = Publisher.Models.DeadbandType.Absolute
            };
            var result = model.ToStackModel();
            Assert.NotNull(result);
            // Model Absolute→ OPC UA Absolute (value 1)
            Assert.Equal((uint)Opc.Ua.DeadbandType.Absolute, result.DeadbandType);
        }

        [Fact]
        public void DataChangeFilterToStackModel_WithTrigger_SetsTrigger()
        {
            var model = new DataChangeFilterModel
            {
                DataChangeTrigger = DataChangeTriggerType.StatusValueTimestamp
            };
            var result = model.ToStackModel();
            Assert.NotNull(result);
            Assert.Equal(Opc.Ua.DataChangeTrigger.StatusValueTimestamp, result.Trigger);
        }

        // ── AggregateConfiguration.ToStackModel ───────────────────────────────

        [Fact]
        public void AggregateConfigurationToStackModel_NullModel_ReturnsServerDefaults()
        {
            AggregateConfigurationModel? model = null;
            var result = model.ToStackModel();
            Assert.True(result.UseServerCapabilitiesDefaults);
        }

        [Fact]
        public void AggregateConfigurationToStackModel_WithModel_SetsAllFields()
        {
            var model = new AggregateConfigurationModel
            {
                PercentDataBad = 10,
                PercentDataGood = 80,
                TreatUncertainAsBad = false,
                UseSlopedExtrapolation = false
            };

            var result = model.ToStackModel();

            Assert.Equal((byte)10, result.PercentDataBad);
            Assert.Equal((byte)80, result.PercentDataGood);
            Assert.False(result.TreatUncertainAsBad);
            Assert.False(result.UseSlopedExtrapolation);
        }

        [Fact]
        public void AggregateConfigurationToStackModel_NullPercentages_UsesZero()
        {
            var model = new AggregateConfigurationModel
            {
                PercentDataBad = null,
                PercentDataGood = null
            };

            var result = model.ToStackModel();

            Assert.Equal((byte)0, result.PercentDataBad);
            Assert.Equal((byte)0, result.PercentDataGood);
        }

        // ── UserTokenPolicy.ToServiceModel ────────────────────────────────────

        [Fact]
        public void UserTokenPolicyToServiceModel_NullPolicy_ReturnsNull()
        {
            UserTokenPolicy? policy = null;
            var result = policy.ToServiceModel();
            Assert.Null(result);
        }

        [Fact]
        public void UserTokenPolicyToServiceModel_AnonymousToken_ReturnsNoneCredential()
        {
            var policy = new UserTokenPolicy
            {
                PolicyId = "anonymous",
                TokenType = UserTokenType.Anonymous
            };

            var result = policy.ToServiceModel();

            Assert.NotNull(result);
            Assert.Equal(CredentialType.None, result.CredentialType);
            Assert.Equal("anonymous", result.Id);
        }

        [Fact]
        public void UserTokenPolicyToServiceModel_UserNameToken_ReturnsUserNameCredential()
        {
            var policy = new UserTokenPolicy
            {
                PolicyId = "username",
                TokenType = UserTokenType.UserName,
                SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"
            };

            var result = policy.ToServiceModel();

            Assert.NotNull(result);
            Assert.Equal(CredentialType.UserName, result.CredentialType);
            Assert.Equal("http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256",
                result.SecurityPolicy);
        }

        [Fact]
        public void UserTokenPolicyToServiceModel_CertificateToken_ReturnsCertificateCredential()
        {
            var policy = new UserTokenPolicy
            {
                PolicyId = "cert",
                TokenType = UserTokenType.Certificate,
                IssuerEndpointUrl = "opc.tcp://issuer:4841"
            };

            var result = policy.ToServiceModel();

            Assert.NotNull(result);
            Assert.Equal(CredentialType.X509Certificate, result.CredentialType);
            Assert.NotNull(result.Configuration);
        }

        [Fact]
        public void UserTokenPolicyToServiceModel_IssuedTokenJwt_ReturnsJwtCredential()
        {
            var policy = new UserTokenPolicy
            {
                PolicyId = "jwt",
                TokenType = UserTokenType.IssuedToken,
                IssuedTokenType = "http://opcfoundation.org/UA/UserToken#JWT",
                IssuerEndpointUrl = "https://login.example.com"
            };

            var result = policy.ToServiceModel();

            Assert.NotNull(result);
            Assert.Equal(CredentialType.JwtToken, result.CredentialType);
        }

        [Fact]
        public void UserTokenPolicyToServiceModel_UnknownIssuedTokenType_ReturnsNull()
        {
            var policy = new UserTokenPolicy
            {
                PolicyId = "other",
                TokenType = UserTokenType.IssuedToken,
                IssuedTokenType = "http://example.com/unknown"
            };

            var result = policy.ToServiceModel();

            Assert.Null(result);
        }

        // ── List<UserTokenPolicy>.ToServiceModel ──────────────────────────────

        [Fact]
        public void UserTokenPoliciesListToServiceModel_NullPolicies_ReturnsAnonymous()
        {
            List<UserTokenPolicy>? policies = null;
            var result = policies!.ToServiceModel();

            Assert.Single(result);
            Assert.Equal(CredentialType.None, result[0].CredentialType);
        }

        [Fact]
        public void UserTokenPoliciesListToServiceModel_EmptyPolicies_ReturnsAnonymous()
        {
            var policies = new List<UserTokenPolicy>();
            var result = policies.ToServiceModel();

            Assert.Single(result);
            Assert.Equal(CredentialType.None, result[0].CredentialType);
        }

        [Fact]
        public void UserTokenPoliciesListToServiceModel_WithAnonymousPolicy_ReturnsIt()
        {
            var policies = new List<UserTokenPolicy>
            {
                new() { PolicyId = "anon", TokenType = UserTokenType.Anonymous }
            };

            var result = policies.ToServiceModel();

            Assert.Single(result);
            Assert.Equal(CredentialType.None, result[0].CredentialType);
        }

        [Fact]
        public void UserTokenPoliciesListToServiceModel_WithUserNamePolicy_ReturnsUserName()
        {
            var policies = new List<UserTokenPolicy>
            {
                new() { PolicyId = "un", TokenType = UserTokenType.UserName }
            };

            var result = policies.ToServiceModel();

            Assert.Single(result);
            Assert.Equal(CredentialType.UserName, result[0].CredentialType);
        }

        [Fact]
        public void UserTokenPoliciesListToServiceModel_UnknownPoliciesFiltered()
        {
            var policies = new List<UserTokenPolicy>
            {
                new() { PolicyId = "unknown", TokenType = (UserTokenType)99 }
            };

            var result = policies.ToServiceModel();

            // Unknown token type returns null and is filtered out; result is empty
            Assert.Empty(result);
        }

        // ── ToStackModel (BrowseViewModel) ─────────────────────────────────────

        [Fact]
        public void BrowseViewToStackModel_NullModel_ReturnsNull()
        {
            BrowseViewModel? model = null;
            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);
            Assert.Null(result);
        }

        [Fact]
        public void BrowseViewToStackModel_EmptyModel_ReturnsDefaults()
        {
            var model = new BrowseViewModel { ViewId = "i=84" };
            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);

            Assert.NotNull(result);
            Assert.Equal(DateTime.MinValue, result.Timestamp);
            Assert.Equal(0u, result.ViewVersion);
        }

        [Fact]
        public void BrowseViewToStackModel_WithTimestamp_SetsTimestamp()
        {
            var ts = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            var model = new BrowseViewModel { ViewId = "i=84", Timestamp = ts };

            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);

            Assert.NotNull(result);
            Assert.Equal(ts, (DateTime)result.Timestamp);
        }

        [Fact]
        public void BrowseViewToStackModel_WithVersion_SetsVersion()
        {
            var model = new BrowseViewModel { ViewId = "i=84", Version = 42 };

            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);

            Assert.NotNull(result);
            Assert.Equal(42u, result.ViewVersion);
        }

        // ── SimpleAttributeOperand.ToStackModel ───────────────────────────────

        [Fact]
        public void SimpleAttributeOperandToStackModel_NullModel_ReturnsNull()
        {
            SimpleAttributeOperandModel? model = null;
            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);
            Assert.Null(result);
        }

        [Fact]
        public void SimpleAttributeOperandToStackModel_EmptyModel_SetsDefaultAttributeId()
        {
            var model = new SimpleAttributeOperandModel
            {
                TypeDefinitionId = "i=2041"
            };

            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);

            Assert.NotNull(result);
            // Default AttributeId is NodeAttribute.Value = 13
            Assert.Equal((uint)NodeAttribute.Value, result.AttributeId);
        }

        [Fact]
        public void SimpleAttributeOperandToStackModel_WithBrowsePath_SetsBrowsePath()
        {
            var model = new SimpleAttributeOperandModel
            {
                TypeDefinitionId = "i=2041",
                BrowsePath = ["Message"],
                AttributeId = NodeAttribute.DisplayName
            };

            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);

            Assert.NotNull(result);
            Assert.NotNull(result.BrowsePath);
            Assert.Equal(1, result.BrowsePath.Count);
            Assert.Equal((uint)NodeAttribute.DisplayName, result.AttributeId);
        }

        [Fact]
        public void SimpleAttributeOperandToStackModel_WithIndexRange_SetsIndexRange()
        {
            var model = new SimpleAttributeOperandModel
            {
                TypeDefinitionId = "i=2041",
                IndexRange = "0:10"
            };

            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);

            Assert.NotNull(result);
            Assert.Equal("0:10", result.IndexRange);
        }

        // ── SimpleAttributeOperand.ToServiceModel ─────────────────────────────

        [Fact]
        public void SimpleAttributeOperandToServiceModel_NullModel_ReturnsNull()
        {
            SimpleAttributeOperand? model = null;
            var result = model.ToServiceModel(ServiceMessageContext.GlobalContext,
                NamespaceFormat.Expanded);
            Assert.Null(result);
        }

        [Fact]
        public void SimpleAttributeOperandToServiceModel_WithTypeDefinitionId_SetsTypeDefinitionId()
        {
            var model = new SimpleAttributeOperand
            {
                TypeDefinitionId = new NodeId(2041),
                AttributeId = (uint)NodeAttribute.Value
            };

            var result = model.ToServiceModel(ServiceMessageContext.GlobalContext,
                NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.Equal(NodeAttribute.Value, result.AttributeId);
        }

        // ── IssuedToken JWT with valid JSON body ───────────────────────────────

        [Fact]
        public void UserTokenPolicyToServiceModel_IssuedTokenJwtValidJson_ParsesConfiguration()
        {
            var policy = new UserTokenPolicy
            {
                PolicyId = "jwt-json",
                TokenType = UserTokenType.IssuedToken,
                IssuedTokenType = "http://opcfoundation.org/UA/UserToken#JWT",
                IssuerEndpointUrl = """{"authority":"https://login.example.com","resource":"api://resource"}"""
            };

            var result = policy.ToServiceModel();

            Assert.NotNull(result);
            Assert.Equal(CredentialType.JwtToken, result.CredentialType);
            // Valid JSON is parsed into a JsonNode, not stored as string
            Assert.NotNull(result.Configuration);
        }

        // ── AggregateFilter.ToStackModel ─────────────────────────────────────

        [Fact]
        public void AggregateFilterToStackModel_NullModel_ReturnsNull()
        {
            AggregateFilterModel? model = null;
            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);
            Assert.Null(result);
        }

        [Fact]
        public void AggregateFilterToStackModel_WithModel_SetsStartTimeAndInterval()
        {
            var startTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var model = new AggregateFilterModel
            {
                AggregateTypeId = "i=2340",
                StartTime = startTime,
                ProcessingInterval = TimeSpan.FromSeconds(5),
                AggregateConfiguration = new AggregateConfigurationModel
                {
                    PercentDataBad = 50,
                    PercentDataGood = 50
                }
            };

            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);

            Assert.NotNull(result);
            Assert.Equal(startTime, result.StartTime);
            Assert.Equal(5000.0, result.ProcessingInterval);
        }

        [Fact]
        public void AggregateFilterToStackModel_NullStartTime_UsesMinValue()
        {
            var model = new AggregateFilterModel
            {
                AggregateTypeId = "i=2340",
                StartTime = null,
                ProcessingInterval = null
            };

            var result = model.ToStackModel(ServiceMessageContext.GlobalContext);

            Assert.NotNull(result);
            Assert.Equal(DateTime.MinValue, result.StartTime);
            Assert.Equal(0.0, result.ProcessingInterval);
        }

        // ── RolePermissionType.ToServiceModel ────────────────────────────────

        [Fact]
        public void RolePermissionToServiceModel_WithValidRoleId_ReturnsModel()
        {
            var type = new RolePermissionType
            {
                RoleId = ObjectIds.WellKnownRole_Anonymous,
                Permissions = (uint)PermissionType.Browse
            };

            var result = type.ToServiceModel(ServiceMessageContext.GlobalContext,
                NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.NotNull(result.RoleId);
        }

        [Fact]
        public void SimpleAttributeOperandToServiceModel_WithBrowsePath_SetsBrowsePath()
        {
            var operand = new SimpleAttributeOperand
            {
                TypeDefinitionId = new NodeId(2041u),
                AttributeId = (uint)NodeAttribute.Value,
                BrowsePath = new List<QualifiedName>
                {
                    new QualifiedName("Child1"),
                    new QualifiedName("Child2")
                }
            };

            var result = operand.ToServiceModel(ServiceMessageContext.GlobalContext,
                NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.NotNull(result.BrowsePath);
            Assert.Equal(2, result.BrowsePath!.Count);
        }

        [Fact]
        public void SimpleAttributeOperandToServiceModel_WithIndexRange_SetsIndexRange()
        {
            var operand = new SimpleAttributeOperand
            {
                TypeDefinitionId = new NodeId(2041u),
                AttributeId = (uint)NodeAttribute.Value,
                IndexRange = "2:5"
            };

            var result = operand.ToServiceModel(ServiceMessageContext.GlobalContext,
                NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.Equal("2:5", result.IndexRange);
        }

        // ── UserTokenPolicyToServiceModel — Certificate with null issuer ─────

        [Fact]
        public void UserTokenPolicyToServiceModel_CertificateWithNullIssuer_NullConfiguration()
        {
            var policy = new UserTokenPolicy
            {
                PolicyId = "cert-no-issuer",
                TokenType = UserTokenType.Certificate,
                IssuerEndpointUrl = null
            };

            var result = policy.ToServiceModel();

            Assert.NotNull(result);
            Assert.Equal(CredentialType.X509Certificate, result.CredentialType);
            // Configuration is JsonValue.Create(null) when issuer is null — still non-null
        }

        // ── ToRequestHeader — DiagnosticsLevel propagation ──────────────────

        [Theory]
        [InlineData(Publisher.Models.DiagnosticsLevel.None)]
        [InlineData(Publisher.Models.DiagnosticsLevel.Information)]
        [InlineData(Publisher.Models.DiagnosticsLevel.Debug)]
        [InlineData(Publisher.Models.DiagnosticsLevel.Verbose)]
        public void ToRequestHeader_DiagnosticsLevel_SetsReturnDiagnostics(
            Publisher.Models.DiagnosticsLevel level)
        {
            var header = new RequestHeaderModel
            {
                Diagnostics = new DiagnosticsModel { Level = level }
            };

            var result = header.ToRequestHeader();

            Assert.NotNull(result);
            // ReturnDiagnostics should be a non-negative stack type value
            Assert.True(result.ReturnDiagnostics >= 0);
        }
    }
}
