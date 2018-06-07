// Copyright (c) Microsoft. All rights reserved.

using WebService.Test.helpers;
using WebService.Test.helpers.Http;
using Xunit.Abstractions;

namespace WebService.Test.IntegrationTests {
    public class ServiceStatusTest
    {
        private readonly HttpClient httpClient;

        // Pull Request don't have access to secret credentials, which are
        // required to run tests interacting with Azure IoT Hub.
        // The tests should run when working locally and when merging branches.
        private readonly bool credentialsAvailable;

        public ServiceStatusTest(ITestOutputHelper log)
        {
            this.httpClient = new HttpClient(log);
            this.credentialsAvailable = !CIVariableHelper.IsPullRequest(log);
        }

     //  [Fact, Trait(Constants.TYPE, Constants.INTEGRATION_TEST)]
     //  public void TheServiceIsHealthy()
     //  {
     //      var request = new HttpRequest();
     //      request.SetUriFromString(AssemblyInitialize.Current.WsHostname + "/v1/status");
     //
     //      // Act
     //      var response = this.httpClient.GetAsync(request).Result;
     //
     //      // Assert
     //      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
     //  }
    }
}
