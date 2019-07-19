// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Tests {

#if false
    [TestCaseOrderer("TestCaseOrdering.PriorityOrderer", "Microsoft.Azure.IIoT.OpcUa.Vault.Tests")]
    public class CertificateAuthorityTests : IClassFixture<CertificateAuthorityTestFixture> {
        public CertificateAuthorityTests(CertificateAuthorityTestFixture fixture, ITestOutputHelper log) {
            _fixture = fixture;
            // fixture
            fixture.SkipOnInvalidConfiguration();
            _logger = SerilogTestLogger.Create<CertificateAuthorityTests>(log);
            _applicationsDatabase = fixture.ApplicationsDatabase;
            _groupServices = fixture.Services;
            _groupRegistry = fixture.Registry;
            _requests = fixture.RequestManagement;
            _ca = fixture.CertificateAuthority;
            _applicationTestSet = fixture.ApplicationTestSet;
            _randomSource = new RandomSource(10815);
        }

        /// <summary>
        /// Test to clean the database from collisions with the test set.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(100)]
        public async Task CleanupAllApplications() {
            _logger.Information("Cleanup All Applications");
            foreach (var application in _applicationTestSet) {
                var applicationModelList = await _applicationsDatabase.ListAllApplicationsAsync();
                Assert.NotNull(applicationModelList);
                foreach (var response in applicationModelList) {
                    try {
                        await _applicationsDatabase.UnregisterApplicationAsync(response.ApplicationId);
                    }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                    catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                }
            }
            _fixture.RegistrationOk = false;
        }

        /// <summary>
        /// Test to register all applications in the test set.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(200)]
        public async Task RegisterAllApplications() {
            foreach (var application in _applicationTestSet) {
                var result = await _applicationsDatabase.RegisterApplicationAsync(
                    application.Model.ToRegistrationRequest());
                var applicationModel = await _applicationsDatabase.GetApplicationAsync(application.Model.ApplicationId);
                Assert.NotNull(applicationModel);
                Assert.NotNull(applicationModel.Application.ApplicationId);
                ApplicationTestData.AssertEqualApplicationModelData(applicationModel.Application, application.Model);
                application.Model = applicationModel.Application;
                Assert.NotNull(applicationModel);
            }
            _fixture.RegistrationOk = true;
        }

        /// <summary>
        /// Initialize certificate request class.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(190)]
        public async Task InitCertificateRequestAndGroup() {
            if (_groupServices is Autofac.IStartable start) {
                start.Start();
            }
            var groups = await _groupRegistry.ListGroupsAsync();
            foreach (var group in groups.Registrations) {
                var cert = await _groupServices.CreateIssuerCertificateAsync(group.Group);
                var chain = await _groupServices.GetIssuerCertificateChainAsync(
                    cert.SerialNumber);
                Assert.NotNull(chain);
                Assert.True(chain.Chain.Count > 0);
            }
        }

        /// <summary>
        /// Create a NewKeyPair request for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1000)]
        public async Task NewKeyPairRequestAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            var count = 0;
            foreach (var application in _applicationTestSet) {
                var groups = await _groupRegistry.ListGroupIdsAsync();
                foreach (var group in groups.Groups) {
                    var applicationId = application.Model.ApplicationId;
                    var requestId = await _ca.StartNewKeyPairRequestAsync(new StartNewKeyPairRequestModel {
                        EntityId = applicationId,
                        GroupId = group,
                        SubjectName = application.Subject,
                        DomainNames = application.DomainNames
                    }, "unittest@opcvault.com");
                    Assert.NotNull(requestId);
                    // read request
                    var request = await _requests.GetRequestAsync(requestId);
                    Assert.Equal(CertificateRequestState.New, request.State);
                    Assert.Equal(requestId, request.RequestId);
                    Assert.Equal(applicationId, request.ApplicationId);
                    Assert.False(request.SigningRequest);
                    Assert.True(Opc.Ua.Utils.CompareDistinguishedName(application.Subject, request.SubjectName));
                    Assert.Equal(group, request.GroupId);
                    Assert.Equal(application.DomainNames.ToArray(), request.DomainNames);

                    // TODO: test all fields
                    application.RequestIds.Add(requestId);
                    count++;
                }
            }
            Assert.True(count > 0);
        }

        /// <summary>
        /// Create a Certificate Signing Request for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1000)]
        public async Task SigningRequestAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            var count = 0;
            foreach (var application in _applicationTestSet) {
                var groups = await _groupRegistry.ListGroupIdsAsync();
                foreach (var group in groups.Groups) {
                    var applicationId = application.Model.ApplicationId;
                    var certificateGroupConfiguration = await _groupRegistry.GetGroupAsync(group);
                    var csrCertificate = CertificateFactory.CreateCertificate(
                        null, null, null,
                        application.ApplicationRecord.ApplicationUri,
                        null,
                        application.Subject,
                        application.DomainNames.ToArray(),
                        /* certificateGroupConfiguration.Policy.IssuedKeySize ?? */ 2048,
                        DateTime.UtcNow.AddDays(-20), 1, 256);
                      //  certificateGroupConfiguration.Policy.IssuedLifetime,
                        //certificateGroupConfiguration.Policy.IssuedSignatureAlgorithm
                       // );
                    var csr = CertificateFactory.CreateSigningRequest(
                        csrCertificate,
                        application.DomainNames);
                    var requestId = await _ca.StartSigningRequestAsync(new StartSigningRequestModel {
                        EntityId = applicationId,
                        GroupId = group,
                        CertificateRequest = csr,
                    }, "unittest@opcvault.com");
                    Assert.NotNull(requestId);
                    // read request
                    var request = await _requests.GetRequestAsync(requestId);
                    Assert.Equal(CertificateRequestState.New, request.State);
                    Assert.True(request.SigningRequest);
                    Assert.Equal(requestId, request.RequestId);
                    Assert.Equal(applicationId, request.ApplicationId);
                    Assert.Null(request.SubjectName);
                    Assert.Equal(group, request.GroupId);
                    //Assert.Equal(null, fullRequest.CertificateType);
                    //Assert.Equal(application.DomainNames.ToArray(), fullRequest.DomainNames);
                    // add to list
                    application.RequestIds.Add(requestId);
                    count++;
                }
            }
            Assert.True(count > 0);
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after StartRequests.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1400)]
        public async Task FetchRequestsAfterStartAllApplications() {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Read certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(1500)]
        public async Task ReadRequestsAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet) {
                foreach (var requestId in application.RequestIds) {
                    var request = await _requests.GetRequestAsync(requestId);
                    Assert.Equal(CertificateRequestState.New, request.State);
                }
            }
        }


        /// <summary>
        /// Approve or reject certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(2000)]
        public async Task ApproveRequestsAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet) {
                foreach (var requestId in application.RequestIds) {
                    var request = await _requests.GetRequestAsync(requestId);
                    Assert.Equal(CertificateRequestState.New, request.State);
                    // approve/reject 50% randomly
                    var reject = _randomSource.NextInt32(100) > 50;
                    if (!reject) {
                        await _requests.ApproveRequestAsync(requestId);
                    }
                    else {
                        await _requests.RejectRequestAsync(requestId);
                    }
                    request = await _requests.GetRequestAsync(requestId);
                    Assert.Equal(reject ? CertificateRequestState.Rejected : CertificateRequestState.Approved, request.State);
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(2000)]
        public async Task FetchRequestsAfterApproveAllApplications() {
            await FetchRequestsAllApplications();
        }

        /// <summary>
        /// Accept the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(3000)]
        public async Task AcceptRequestsAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet) {
                foreach (var requestId in application.RequestIds) {
                    var appModel = application.Model;
                    var applicationId = application.Model.ApplicationId;
                    var request = await _requests.GetRequestAsync(requestId);
                    if (request.State == CertificateRequestState.Approved) {
                        await _requests.AcceptRequestAsync(requestId);
                        request = await _requests.GetRequestAsync(requestId);
                        Assert.Equal(CertificateRequestState.Accepted, request.State);
                    }
                    else {
                        await Assert.ThrowsAsync<ResourceInvalidStateException>(() => _requests.AcceptRequestAsync(requestId));
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(3100)]
        public async Task FetchRequestsAfterAcceptAllApplications() {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Delete the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(4000)]
        public async Task DeleteRequestsHalfApplications() {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet) {
                foreach (var requestId in application.RequestIds) {
                    if (_randomSource.NextInt32(100) > 50) {
                        var request = await _requests.GetRequestAsync(requestId);
                        if (request.State == CertificateRequestState.New ||
                            request.State == CertificateRequestState.Rejected ||
                            request.State == CertificateRequestState.Approved ||
                            request.State == CertificateRequestState.Accepted) {
                            await _requests.RemoveRequestAsync(requestId);
                        }
                        else {
                            await Assert.ThrowsAsync<ResourceInvalidStateException>(() => _requests.RemoveRequestAsync(requestId));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(4500)]
        public async Task FetchRequestsAfterDeleteHalfApplications() {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Revoke the certificate requests for all deleted applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5000)]
        public async Task RevokeRequestsAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet) {
                foreach (var requestId in application.RequestIds) {
                    var request = await _requests.GetRequestAsync(requestId);
                    if (request.State == CertificateRequestState.Deleted) {
                        await _requests.RevokeRequestCertificateAsync(requestId);
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5100)]
        public async Task FetchRequestsAfterRevokeAllApplications() {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Delete the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5400)]
        public async Task DeleteRequestsAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet) {
                foreach (var requestId in application.RequestIds) {
                    var request = await _requests.GetRequestAsync(requestId);
                    if (request.State == CertificateRequestState.New ||
                        request.State == CertificateRequestState.Rejected ||
                        request.State == CertificateRequestState.Approved ||
                        request.State == CertificateRequestState.Accepted) {
                        await _requests.RemoveRequestAsync(requestId);
                    }
                    else {
                        await Assert.ThrowsAsync<ResourceInvalidStateException>(() => _requests.RemoveRequestAsync(requestId));
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5500)]
        public async Task FetchRequestsAfterDeleteAllApplications() {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// RevokeGroup the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5600)]
        public async Task RevokeGroupRequestsAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            var groups = await _groupRegistry.ListGroupIdsAsync();
            foreach (var group in groups.Groups) {
                await _requests.RevokeAllRequestsAsync(group, true);
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Approve.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5700)]
        public async Task FetchRequestsAfterRevokeGroupAllApplications() {
            await FetchRequestsAllApplications();
        }


        /// <summary>
        /// Purge the certificate requests for all applications.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5800)]
        public async Task PurgeRequestsAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet) {
                foreach (var requestId in application.RequestIds) {
                    var request = await _requests.GetRequestAsync(requestId);
                    if (request.State == CertificateRequestState.Revoked ||
                        request.State == CertificateRequestState.Rejected ||
                        request.State == CertificateRequestState.Completed ||
                        request.State == CertificateRequestState.New) {
                        await _requests.DeleteRequestAsync(requestId);
                    }
                    else {
                        await Assert.ThrowsAsync<ResourceInvalidStateException>(() => _requests.DeleteRequestAsync(requestId));
                    }
                }
            }
        }

        /// <summary>
        /// Fetch the certificate requests for all applications after Purge.
        /// </summary>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(5900)]
        public async Task FetchRequestsAfterPurgeAllApplications() {
            await FetchRequestsAllApplications(true);
        }

        /// <summary>
        /// Unregister all applications, clean up test set.
        /// </summary>
        /// <returns></returns>
        [SkippableFact, Trait(Constants.Type, Constants.UnitTest), TestPriority(6000)]
        public async Task UnregisterAllApplications() {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet) {
                await _applicationsDatabase.UnregisterApplicationAsync(
                    application.Model.ApplicationId);
            }
        }

        /// <summary>
        /// Test helper to test fetch for various states of requests in the workflow.
        /// </summary>
        private async Task FetchRequestsAllApplications(bool purged = false) {
            Skip.If(!_fixture.RegistrationOk);
            foreach (var application in _applicationTestSet) {
                foreach (var requestId in application.RequestIds) {
                    var appModel = application.Model;
                    if (purged) {
                        await Assert.ThrowsAsync<ResourceNotFoundException>(
                            () => _ca.FinishSigningRequestAsync(requestId));
                        continue;
                    }
                    var fetchResult = await _ca.FinishSigningRequestAsync(requestId);
                    Assert.Equal(requestId, fetchResult.Request.RequestId);
                    Assert.Equal(application.Model.ApplicationId, fetchResult.Request.EntityId);
                    if (fetchResult.Request.State == CertificateRequestState.Approved ||
                        fetchResult.Request.State == CertificateRequestState.Accepted) {
                        Assert.NotNull(fetchResult.Certificate);
                    }
                    else if (fetchResult.Request.State == CertificateRequestState.Revoked ||
                        fetchResult.Request.State == CertificateRequestState.Deleted) {
                        Assert.Null(fetchResult.PrivateKey);
                    }
                    else if (fetchResult.Request.State == CertificateRequestState.Rejected ||
                        fetchResult.Request.State == CertificateRequestState.New ||
                        fetchResult.Request.State == CertificateRequestState.Completed
                        ) {
                        Assert.Null(fetchResult.PrivateKey);
                        Assert.Null(fetchResult.Certificate);
                    }
                    else {
                        Assert.True(false, "Invalid State");
                    }
                }
            }
        }

        private readonly CertificateAuthorityTestFixture _fixture;
        private readonly ILogger _logger;
        private readonly IApplicationRegistry _applicationsDatabase;
        private readonly ITrustGroupStore _groupRegistry;
        private readonly ITrustGroupServices _groupServices;
        private readonly IRequestManagement _requests;
        private readonly ISigningRequestProcessor _ca;
        private readonly IList<ApplicationTestData> _applicationTestSet;
        private readonly RandomSource _randomSource;
    }
#endif
}
