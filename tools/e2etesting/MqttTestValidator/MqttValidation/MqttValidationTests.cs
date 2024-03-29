// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MqttValidation
{
    using FluentAssertions;
    using NUnit.Framework;
    using System;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class MqttValidationTests
    {

        private const string EndpointKey = "VERIFIER_ENDPOINT";
        private const string BrokerEndpointKey = "BROKER_ENDPOINT";
        private const string BrokerPortKey = "BROKER_PORT";
        private const string TopicKey = "TOPIC";
        private const string TimeToObserveKey = "TIME_TO_OBSERVE";

        private swaggerClient? _swaggerClient;

        [SetUp]
        public void Setup()
        {
            var endpoint = Environment.GetEnvironmentVariable(EndpointKey);
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Assert.Ignore($"Environment variable '{EndpointKey}' not specified!");

            }

            var httpClient = new HttpClient();
            _swaggerClient = new swaggerClient(endpoint, httpClient);

        }

        [Test]
        public async Task SimpleVerification()
        {
            ArgumentNullException.ThrowIfNull(_swaggerClient);

            static void SetFromEnv(Action<string> setter, string key)
            {
                var value = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    setter(value);
                }
            }

            var request = new MqttVerificationRequest();

            SetFromEnv(v => request.MqttBroker = v, BrokerEndpointKey);
            SetFromEnv(v => request.MqttPort = int.Parse(v, CultureInfo.InvariantCulture), BrokerPortKey);
            SetFromEnv(v => request.MqttTopic = v, TopicKey);
            SetFromEnv(v => request.TimeToObserve = int.Parse(v, CultureInfo.InvariantCulture), TimeToObserveKey);

            var verificationTask = await _swaggerClient.StartVerificationAsync(request).ConfigureAwait(false);


            MqttVerificationDetailedResponse result;

            do
            {
                result = await _swaggerClient.GetVerificationResultAsync(verificationTask.ValidationTaskId).ConfigureAwait(false);

                if (result.IsFinished)
                {
                    break;
                }
                else
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            } while (true);

            result.Error.Should().BeNullOrEmpty();
            result.NumberOfMessages.Should().BeGreaterThan(5);
        }
    }
}
