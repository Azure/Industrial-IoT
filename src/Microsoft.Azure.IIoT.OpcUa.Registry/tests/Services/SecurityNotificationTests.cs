// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Autofac.Extras.Moq;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using System;

    public class SecurityNotificationTests {

        [Fact]
        public void SendSecurityAlertWhenEndpointModeUnsecure() {
            SecurityMode mode = SecurityMode.None;
            var certString = "MIIERTCCAy2gAwIBAgIBAzANBgkqhkiG9w0BAQsFADBsMQswCQYDVQQGEwJVUzEQMA4GA1UECAwHQXJpem9uYTEXMBUGA1UECgwOT1BDIEZvdW5kYXRpb24xETAPBgNVBAMMCGN0dF9jYTFVMR8wHQYKCZImiZPyLGQBGRYPREUtVEVDSExJTksyNDFCMB4XDTE5MDYwNjEzMDU1OVoXDTIwMDYwNTEzMDU1OVowcjELMAkGA1UEBhMCVVMxEDAOBgNVBAgMB0FyaXpvbmExFzAVBgNVBAoMDk9QQyBGb3VuZGF0aW9uMRcwFQYDVQQDDA5jdHRfY2ExVV9hcHBUUjEfMB0GCgmSJomT8ixkARkWD0RFLVRFQ0hMSU5LMjQxQjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALMFn9m22HhaqTqYwFgdIfL5hyB5z40HfFex0560+hlVfmP2mjy8RfSIubi5RJ/miqTi7gqVAxEHscz0CFpe8LFDX2/rnJX/i+nYw0tP8doZ9eQKyxQarm1R/V9htw5/1Qojyie+PV9CBR5DxqKORwjlF/4Lf1ZSumZ92OTTbkgcx8ZY5pYz2n8Z1hv2oZzK/grSOTmbubLXuA99lQZrf4UKP9XjKgbaqDcgjL3TnsHVy6qPno1NLJbRzmmY+KoSJYVf2xt6eOYp7sNhO5lSEpVWWUKpr8Hd2JDvOKAc24xNeYS5/wGz0dKhe9aISNZtD41hoeEwsZ7l5eTJpAFtY4cCAwEAAaOB6zCB6DAsBglghkgBhvhCAQ0EHxYdT3BlblNTTCBHZW5lcmF0ZWQgQ2VydGlmaWNhdGUwHQYDVR0OBBYEFOrViGHzcc5zbrN72b4Ofrj2sIPsMB8GA1UdIwQYMBaAFM8VlTNftOdGrzoweARhS1h8oTdeMEEGA1UdEQQ6MDiGNnVybjpERS1URUNITElOSzI0MUI6T1BDRm91bmRhdGlvbjpVYUNvbXBsaWFuY2VUZXN0VG9vbDALBgNVHQ8EBAMCBPAwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMAkGA1UdEwQCMAAwDQYJKoZIhvcNAQELBQADggEBAANnZNbSID0zAjdKbxSWSP048NTgu5R38UHsQHone0S9quMQFbVUykl8SVUL1NhC28ObTqFuUJooJusGeNNvQjFWVW8qZIp/x7UHZvT9mSduju92fITkAcF9QFdcVCSeN3KV0Xq0scDX57zFYL6vEiXVHa8wWrAZqSs0lYfaZj4XMa8tABhj4Tzwhe+THonnAkH//KPCkaYpV3IeSfP0JbvQ74+xKzlzcPdPTrqGwbZw3ExQK8XwE9r2i6X8negmEl4wmHmtx2cTPp7Vwy/OpxfWnMGr8dJBz2dlpZpYHvQkhWcLCVR3m/Tv3EWy5YAn3/FpEvW2XkdNhTSK6aOVGHg=";
            byte[] certEncoded = Convert.FromBase64String(certString);

            CreateEndpointFixtures(mode, "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", certEncoded, out var endpoints);

            using (var mock = AutoMock.GetLoose()) {
                var mockIotTelemetryService = new IoTHubServices(null);
                mock.Provide<IIoTHubTelemetryServices>(mockIotTelemetryService);
                var service = mock.Create<SecurityNotificationService>();

                // Run
                var t = service.OnEndpointAddedAsync(endpoints.FirstOrDefault().Registration);

                // Assert
                Assert.True(t.IsCompletedSuccessfully);
                mockIotTelemetryService.Events.TryTake(out var eventMessage);
                eventMessage.Message.Properties.TryGetValue(SystemProperties.InterfaceId, out string val);
                Assert.Equal("http://security.azureiot.com/SecurityAgent/1.0.0", val);
            }
        }

        [Fact]
        public void SendSecurityAlertWhenEndpointPolicyUnsecure() {
            SecurityMode mode = SecurityMode.Best;
            var certString = "MIIEWjCCA0KgAwIBAgIBADANBgkqhkiG9w0BAQsFADBsMQswCQYDVQQGEwJVUzEQMA4GA1UECAwHQXJpem9uYTEXMBUGA1UECgwOT1BDIEZvdW5kYXRpb24xETAPBgNVBAMMCGN0dF9jYTFUMR8wHQYKCZImiZPyLGQBGRYPREUtVEVDSExJTksyNDFCMB4XDTE5MDYwNjEzMDUxMVoXDTI0MDYwNDEzMDUxMVowbDELMAkGA1UEBhMCVVMxEDAOBgNVBAgMB0FyaXpvbmExFzAVBgNVBAoMDk9QQyBGb3VuZGF0aW9uMREwDwYDVQQDDAhjdHRfY2ExVDEfMB0GCgmSJomT8ixkARkWD0RFLVRFQ0hMSU5LMjQxQjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALW6H1EMBMco / WcrNQB6QEt3peBhNLjOsm7ZYLBZvf6MKzGOWNAxDAB3yVku7DNX4YdWaS3eD3woPcvXuTtnUUV6FJcvg5KnjyNb0dVxRpGgbybTUm2dujcPLtDNq18RJggnM7 + Y937PKanEdzvB0XBuPgFam2pDR4fDpy4myL4JFIl / y7cihlevhSDIC3o2Q85AEqVH7oMn6a2FSRil3qDFnILUPgdpIz2WtRF4Niy748r3iy6ge1B0Z8STWwZqcefhd4bMMyHy7QKSxWIdLPlQJMhhBt143If9BklNvBBS10JhKgos7nFRpRB4GN2P0mnd + 5airnejBkJFpgmSMacCAwEAAaOCAQUwggEBMCwGCWCGSAGG + EIBDQQfFh1PcGVuU1NMIEdlbmVyYXRlZCBDZXJ0aWZpY2F0ZTAdBgNVHQ4EFgQU812Ve8TzfzwptHanfRCngFNsKlEwgZYGA1UdIwSBjjCBi4AU812Ve8TzfzwptHanfRCngFNsKlGhcKRuMGwxCzAJBgNVBAYTAlVTMRAwDgYDVQQIDAdBcml6b25hMRcwFQYDVQQKDA5PUEMgRm91bmRhdGlvbjERMA8GA1UEAwwIY3R0X2NhMVQxHzAdBgoJkiaJk / IsZAEZFg9ERS1URUNITElOSzI0MUKCAQAwDAYDVR0TBAUwAwEB / zALBgNVHQ8EBAMCAYYwDQYJKoZIhvcNAQELBQADggEBAIY720uKPgLHsG1KD29VE6Yvs5HXsJ7FCYtGU6Mjm / UbcJr5WSfVy4exBcmlHGQ2 + AfmvCLoPTZv + GVrGlCLnc4RRe2wBVHf9Bbaj7hcUGhvW7b0nZachYIOnvQut / o / U7XAJVuwgnzPwyO7EmDzQClCJ7zL3MonO4o0buqT7aV + SkstwW89UDIkOV + 5TXTC / Tq22eVyRhhOzmpBC21hrkzhCVvwrYCsb3vGaKSnahil7A59PyMb854lekngfH3bPyw1oQoGRpbN9ozkcTKjkENfLiseQd02P8pxaDFluSsnne0MMzFXJOMFbvbhOX4VmHio+ YrK/PFpAOMna2bjEbQ =";
            byte[] certEncoded = Convert.FromBase64String(certString);

            CreateEndpointFixtures(mode, "http://opcfoundation.org/UA/SecurityPolicy#None", certEncoded, out var endpoints);

            using (var mock = AutoMock.GetLoose()) {
                var mockIotTelemetryService = new IoTHubServices(null);
                mock.Provide<IIoTHubTelemetryServices>(mockIotTelemetryService);
                var service = mock.Create<SecurityNotificationService>();

                // Run
                var t = service.OnEndpointAddedAsync(endpoints.FirstOrDefault().Registration);

                // Assert
                Assert.True(t.IsCompletedSuccessfully);
                mockIotTelemetryService.Events.TryTake(out var eventMessage);
                eventMessage.Message.Properties.TryGetValue(SystemProperties.InterfaceId, out string val);
                Assert.Equal("http://security.azureiot.com/SecurityAgent/1.0.0", val);
            }
        }

        [Fact]
        public void DoNotSendSecurityAlertWhenEndpointPolicyAndModeSecure() {
            SecurityMode mode = SecurityMode.Best;
            var certString = "MIIERTCCAy2gAwIBAgIBAzANBgkqhkiG9w0BAQsFADBsMQswCQYDVQQGEwJVUzEQMA4GA1UECAwHQXJpem9uYTEXMBUGA1UECgwOT1BDIEZvdW5kYXRpb24xETAPBgNVBAMMCGN0dF9jYTFVMR8wHQYKCZImiZPyLGQBGRYPREUtVEVDSExJTksyNDFCMB4XDTE5MDYwNjEzMDU1OVoXDTIwMDYwNTEzMDU1OVowcjELMAkGA1UEBhMCVVMxEDAOBgNVBAgMB0FyaXpvbmExFzAVBgNVBAoMDk9QQyBGb3VuZGF0aW9uMRcwFQYDVQQDDA5jdHRfY2ExVV9hcHBUUjEfMB0GCgmSJomT8ixkARkWD0RFLVRFQ0hMSU5LMjQxQjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALMFn9m22HhaqTqYwFgdIfL5hyB5z40HfFex0560+hlVfmP2mjy8RfSIubi5RJ/miqTi7gqVAxEHscz0CFpe8LFDX2/rnJX/i+nYw0tP8doZ9eQKyxQarm1R/V9htw5/1Qojyie+PV9CBR5DxqKORwjlF/4Lf1ZSumZ92OTTbkgcx8ZY5pYz2n8Z1hv2oZzK/grSOTmbubLXuA99lQZrf4UKP9XjKgbaqDcgjL3TnsHVy6qPno1NLJbRzmmY+KoSJYVf2xt6eOYp7sNhO5lSEpVWWUKpr8Hd2JDvOKAc24xNeYS5/wGz0dKhe9aISNZtD41hoeEwsZ7l5eTJpAFtY4cCAwEAAaOB6zCB6DAsBglghkgBhvhCAQ0EHxYdT3BlblNTTCBHZW5lcmF0ZWQgQ2VydGlmaWNhdGUwHQYDVR0OBBYEFOrViGHzcc5zbrN72b4Ofrj2sIPsMB8GA1UdIwQYMBaAFM8VlTNftOdGrzoweARhS1h8oTdeMEEGA1UdEQQ6MDiGNnVybjpERS1URUNITElOSzI0MUI6T1BDRm91bmRhdGlvbjpVYUNvbXBsaWFuY2VUZXN0VG9vbDALBgNVHQ8EBAMCBPAwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMAkGA1UdEwQCMAAwDQYJKoZIhvcNAQELBQADggEBAANnZNbSID0zAjdKbxSWSP048NTgu5R38UHsQHone0S9quMQFbVUykl8SVUL1NhC28ObTqFuUJooJusGeNNvQjFWVW8qZIp/x7UHZvT9mSduju92fITkAcF9QFdcVCSeN3KV0Xq0scDX57zFYL6vEiXVHa8wWrAZqSs0lYfaZj4XMa8tABhj4Tzwhe+THonnAkH//KPCkaYpV3IeSfP0JbvQ74+xKzlzcPdPTrqGwbZw3ExQK8XwE9r2i6X8negmEl4wmHmtx2cTPp7Vwy/OpxfWnMGr8dJBz2dlpZpYHvQkhWcLCVR3m/Tv3EWy5YAn3/FpEvW2XkdNhTSK6aOVGHg=";
            byte[] certEncoded = Convert.FromBase64String(certString);

            CreateEndpointFixtures(mode, "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", certEncoded, out var endpoints);

            using (var mock = AutoMock.GetLoose()) {
                var mockIotTelemetryService = new IoTHubServices(null);
                mock.Provide<IIoTHubTelemetryServices>(mockIotTelemetryService);
                var service = mock.Create<SecurityNotificationService>();

                // Run
                var t = service.OnEndpointAddedAsync(endpoints.FirstOrDefault().Registration);

                // Assert
                Assert.True(t.IsCompletedSuccessfully);
                mockIotTelemetryService.Events.TryTake(out var eventMessage);
                Assert.Null(eventMessage);
            }
        }

        [Fact]
        public void SendSecurityAlertWhenCertificateSelfSigned() {
            SecurityMode mode = SecurityMode.Best;
            var certString = "MIIErzCCA5egAwIBAgIIYWAKpUs1GkIwDQYJKoZIhvcNAQELBQAwcDEbMBkGCgmSJomT8ixkARkWC21vdmFyc2huLXBjMRcwFQYDVQQKDA5PUEMgRm91bmRhdGlvbjEQMA4GA1UECAwHQXJpem9uYTELMAkGA1UEBhMCVVMxGTAXBgNVBAMMEFVBIFNhbXBsZSBTZXJ2ZXIwHhcNMTkwNTA3MTEzODQ0WhcNMjAwNTA3MTEzODQ0WjBwMRswGQYKCZImiZPyLGQBGRYLbW92YXJzaG4tcGMxFzAVBgNVBAoMDk9QQyBGb3VuZGF0aW9uMRAwDgYDVQQIDAdBcml6b25hMQswCQYDVQQGEwJVUzEZMBcGA1UEAwwQVUEgU2FtcGxlIFNlcnZlcjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMFZXwtO5NI2Xmej5O/Xx6I/4s23HrOGtDUFK60w5Lc6E1A4piVOzFd3asouDQcspahkJwxwuMCWT5s8bGNvvzmayT5cSKD+yJQd8BQQKnjSiwoIORPJYD1Htnb3ro5BIModelv+GUpLj6y1ZzHGk6EpcLcMB93Wi+wXLfSDQfGmL0vBWjsTWjoreNvgSNgDAnLuieRA3gBmLlJ7frL+HAXhPBxcg5rnVMJEIQWygiVV7tHKe/Nwdr/P13z0q1Onx0btGz3j+bszFIzJWswjsnZhbavOscrzixe9xQz7Mx+K72UcMQkoMP0zamD1BocPe9VcTwa4YKY1nNV7uPrBSwcCAwEAAaOCAUswggFHMB0GA1UdDgQWBBROnNs77E3dWvQPTx0IYU3/xOb9ZDAMBgNVHRMBAf8EAjAAMIGhBgNVHSMEgZkwgZaAFE6c2zvsTd1a9A9PHQhhTf/E5v1koXSkcjBwMRswGQYKCZImiZPyLGQBGRYLbW92YXJzaG4tcGMxFzAVBgNVBAoMDk9QQyBGb3VuZGF0aW9uMRAwDgYDVQQIDAdBcml6b25hMQswCQYDVQQGEwJVUzEZMBcGA1UEAwwQVUEgU2FtcGxlIFNlcnZlcoIIYWAKpUs1GkIwDgYDVR0PAQH/BAQDAgL0MCAGA1UdJQEB/wQWMBQGCCsGAQUFBwMBBggrBgEFBQcDAjBCBgNVHREEOzA5hip1cm46bW92YXJzaG4tcGM6T1BDRm91bmRhdGlvbjpTYW1wbGVTZXJ2ZXKCC21vdmFyc2huLXBjMA0GCSqGSIb3DQEBCwUAA4IBAQCaTYeEKvd70RW+NcGFzwn6fTXqdlcsucjGSu5VJBvATNWZddWudwnSjBc/Fqy+9uphYcgbBDdO+sEB1Y4nJ9LibfORbuomdAlnXsxeLHBO4n9UyDpGS/ESjvuWtYdNfsMWlQL9j3tYJhp4CEqJGU9xjR74eg9Tdwo8H0fwwxAykmWF7OLpeio+Ig4DNWFr0bN9nDi8KQ9caPhMZ3+X+c3yPMylELFaxFyQnVuD+K4sN/TDli56dZLwqiV3xU6eyJ2F9Qi4PBBqzroLblUk8mtVqLsjj5C8Zw6z/C5vrUlfCrP1vB9yCZY4G24ue6TWy2SPNjRFES/8WvHmHbZ5D/VD";
            byte[] certEncoded = Convert.FromBase64String(certString);

            CreateEndpointFixtures(mode, "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", certEncoded, out var endpoints);

            using (var mock = AutoMock.GetLoose()) {
                var mockIotTelemetryService = new IoTHubServices(null);
                mock.Provide<IIoTHubTelemetryServices>(mockIotTelemetryService);
                var service = mock.Create<SecurityNotificationService>();

                // Run
                var t = service.OnEndpointAddedAsync(endpoints.FirstOrDefault().Registration);

                // Assert

                Assert.True(t.IsCompletedSuccessfully);
                mockIotTelemetryService.Events.TryTake(out var eventMessage);
                eventMessage.Message.Properties.TryGetValue(SystemProperties.InterfaceId, out string val);
                Assert.Equal("http://security.azureiot.com/SecurityAgent/1.0.0", val);
            }
        }

        [Fact]
        public void SendSecurityAlertWhenCertificateExpired() {
            SecurityMode mode = SecurityMode.Best;
            var certString = "MIIErzCCA5egAwIBAgIIYWAKpUs1GkIwDQYJKoZIhvcNAQELBQAwcDEbMBkGCgmSJomT8ixkARkWC21vdmFyc2huLXBjMRcwFQYDVQQKDA5PUEMgRm91bmRhdGlvbjEQMA4GA1UECAwHQXJpem9uYTELMAkGA1UEBhMCVVMxGTAXBgNVBAMMEFVBIFNhbXBsZSBTZXJ2ZXIwHhcNMTkwNTA3MTEzODQ0WhcNMjAwNTA3MTEzODQ0WjBwMRswGQYKCZImiZPyLGQBGRYLbW92YXJzaG4tcGMxFzAVBgNVBAoMDk9QQyBGb3VuZGF0aW9uMRAwDgYDVQQIDAdBcml6b25hMQswCQYDVQQGEwJVUzEZMBcGA1UEAwwQVUEgU2FtcGxlIFNlcnZlcjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMFZXwtO5NI2Xmej5O/Xx6I/4s23HrOGtDUFK60w5Lc6E1A4piVOzFd3asouDQcspahkJwxwuMCWT5s8bGNvvzmayT5cSKD+yJQd8BQQKnjSiwoIORPJYD1Htnb3ro5BIModelv+GUpLj6y1ZzHGk6EpcLcMB93Wi+wXLfSDQfGmL0vBWjsTWjoreNvgSNgDAnLuieRA3gBmLlJ7frL+HAXhPBxcg5rnVMJEIQWygiVV7tHKe/Nwdr/P13z0q1Onx0btGz3j+bszFIzJWswjsnZhbavOscrzixe9xQz7Mx+K72UcMQkoMP0zamD1BocPe9VcTwa4YKY1nNV7uPrBSwcCAwEAAaOCAUswggFHMB0GA1UdDgQWBBROnNs77E3dWvQPTx0IYU3/xOb9ZDAMBgNVHRMBAf8EAjAAMIGhBgNVHSMEgZkwgZaAFE6c2zvsTd1a9A9PHQhhTf/E5v1koXSkcjBwMRswGQYKCZImiZPyLGQBGRYLbW92YXJzaG4tcGMxFzAVBgNVBAoMDk9QQyBGb3VuZGF0aW9uMRAwDgYDVQQIDAdBcml6b25hMQswCQYDVQQGEwJVUzEZMBcGA1UEAwwQVUEgU2FtcGxlIFNlcnZlcoIIYWAKpUs1GkIwDgYDVR0PAQH/BAQDAgL0MCAGA1UdJQEB/wQWMBQGCCsGAQUFBwMBBggrBgEFBQcDAjBCBgNVHREEOzA5hip1cm46bW92YXJzaG4tcGM6T1BDRm91bmRhdGlvbjpTYW1wbGVTZXJ2ZXKCC21vdmFyc2huLXBjMA0GCSqGSIb3DQEBCwUAA4IBAQCaTYeEKvd70RW+NcGFzwn6fTXqdlcsucjGSu5VJBvATNWZddWudwnSjBc/Fqy+9uphYcgbBDdO+sEB1Y4nJ9LibfORbuomdAlnXsxeLHBO4n9UyDpGS/ESjvuWtYdNfsMWlQL9j3tYJhp4CEqJGU9xjR74eg9Tdwo8H0fwwxAykmWF7OLpeio+Ig4DNWFr0bN9nDi8KQ9caPhMZ3+X+c3yPMylELFaxFyQnVuD+K4sN/TDli56dZLwqiV3xU6eyJ2F9Qi4PBBqzroLblUk8mtVqLsjj5C8Zw6z/C5vrUlfCrP1vB9yCZY4G24ue6TWy2SPNjRFES/8WvHmHbZ5D/VD";
            byte[] certEncoded = Convert.FromBase64String(certString);

            CreateEndpointFixtures(mode, "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", certEncoded, out var endpoints);

            using (var mock = AutoMock.GetLoose()) {
                var mockIotTelemetryService = new IoTHubServices(null);
                mock.Provide<IIoTHubTelemetryServices>(mockIotTelemetryService);
                var service = mock.Create<SecurityNotificationService>();

                // Run
                var t = service.OnEndpointAddedAsync(endpoints.FirstOrDefault().Registration);

                // Assert

                Assert.True(t.IsCompletedSuccessfully);
                mockIotTelemetryService.Events.TryTake(out var eventMessage);
                eventMessage.Message.Properties.TryGetValue(SystemProperties.InterfaceId, out string val);
                Assert.Equal("http://security.azureiot.com/SecurityAgent/1.0.0", val);
            }
        }


        [Fact]
        public void DoNotSendSecurityAlertWhenCertificateValid() {
            SecurityMode mode = SecurityMode.Best;
            var certString = "MIIERTCCAy2gAwIBAgIBAzANBgkqhkiG9w0BAQsFADBsMQswCQYDVQQGEwJVUzEQMA4GA1UECAwHQXJpem9uYTEXMBUGA1UECgwOT1BDIEZvdW5kYXRpb24xETAPBgNVBAMMCGN0dF9jYTFVMR8wHQYKCZImiZPyLGQBGRYPREUtVEVDSExJTksyNDFCMB4XDTE5MDYwNjEzMDU1OVoXDTIwMDYwNTEzMDU1OVowcjELMAkGA1UEBhMCVVMxEDAOBgNVBAgMB0FyaXpvbmExFzAVBgNVBAoMDk9QQyBGb3VuZGF0aW9uMRcwFQYDVQQDDA5jdHRfY2ExVV9hcHBUUjEfMB0GCgmSJomT8ixkARkWD0RFLVRFQ0hMSU5LMjQxQjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALMFn9m22HhaqTqYwFgdIfL5hyB5z40HfFex0560+hlVfmP2mjy8RfSIubi5RJ/miqTi7gqVAxEHscz0CFpe8LFDX2/rnJX/i+nYw0tP8doZ9eQKyxQarm1R/V9htw5/1Qojyie+PV9CBR5DxqKORwjlF/4Lf1ZSumZ92OTTbkgcx8ZY5pYz2n8Z1hv2oZzK/grSOTmbubLXuA99lQZrf4UKP9XjKgbaqDcgjL3TnsHVy6qPno1NLJbRzmmY+KoSJYVf2xt6eOYp7sNhO5lSEpVWWUKpr8Hd2JDvOKAc24xNeYS5/wGz0dKhe9aISNZtD41hoeEwsZ7l5eTJpAFtY4cCAwEAAaOB6zCB6DAsBglghkgBhvhCAQ0EHxYdT3BlblNTTCBHZW5lcmF0ZWQgQ2VydGlmaWNhdGUwHQYDVR0OBBYEFOrViGHzcc5zbrN72b4Ofrj2sIPsMB8GA1UdIwQYMBaAFM8VlTNftOdGrzoweARhS1h8oTdeMEEGA1UdEQQ6MDiGNnVybjpERS1URUNITElOSzI0MUI6T1BDRm91bmRhdGlvbjpVYUNvbXBsaWFuY2VUZXN0VG9vbDALBgNVHQ8EBAMCBPAwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMAkGA1UdEwQCMAAwDQYJKoZIhvcNAQELBQADggEBAANnZNbSID0zAjdKbxSWSP048NTgu5R38UHsQHone0S9quMQFbVUykl8SVUL1NhC28ObTqFuUJooJusGeNNvQjFWVW8qZIp/x7UHZvT9mSduju92fITkAcF9QFdcVCSeN3KV0Xq0scDX57zFYL6vEiXVHa8wWrAZqSs0lYfaZj4XMa8tABhj4Tzwhe+THonnAkH//KPCkaYpV3IeSfP0JbvQ74+xKzlzcPdPTrqGwbZw3ExQK8XwE9r2i6X8negmEl4wmHmtx2cTPp7Vwy/OpxfWnMGr8dJBz2dlpZpYHvQkhWcLCVR3m/Tv3EWy5YAn3/FpEvW2XkdNhTSK6aOVGHg=";
            byte[] certEncoded = Convert.FromBase64String(certString);

            CreateEndpointFixtures(mode, "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", certEncoded, out var endpoints);

            using (var mock = AutoMock.GetLoose()) {
                var mockIotTelemetryService = new IoTHubServices(null);
                mock.Provide<IIoTHubTelemetryServices>(mockIotTelemetryService);
                var service = mock.Create<SecurityNotificationService>();

                // Run
                var t = service.OnEndpointAddedAsync(endpoints.FirstOrDefault().Registration);

                // Assert

                Assert.True(t.IsCompletedSuccessfully);
                mockIotTelemetryService.Events.TryTake(out var eventMessage);
                Assert.Null(eventMessage);
            }
        }

        /// <summary>
        /// Helper to create app fixtures
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="policy"></param>
        /// <param name="endpoints"></param>
        private static void CreateEndpointFixtures(SecurityMode mode, string policy, byte[] certificate, out List<EndpointInfoModel> endpoints) {
            var fix = new Fixture();
            fix.Customizations.Add(new TypeRelay(typeof(JToken), typeof(JObject)));
            var superx = fix.Create<string>();
            endpoints = fix
                .Build<EndpointInfoModel>()
                .Without(x => x.Registration)
                .Do(x => x.Registration = fix
                    .Build<EndpointRegistrationModel>()
                    .With(y => y.SupervisorId, superx)
                    .With(y => y.Certificate, certificate)
                    .Without(y => y.Endpoint)
                    .Do(y => y.Endpoint = fix
                        .Build<EndpointModel>()
                        .With(z => z.SecurityMode, mode)
                        .With(z => z.SecurityPolicy, policy)
                        .Create())
                    .Create())
                .CreateMany(1)
                .ToList();
        }
    }
}
