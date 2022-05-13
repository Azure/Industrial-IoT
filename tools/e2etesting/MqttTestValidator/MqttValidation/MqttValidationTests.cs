using FluentAssertions;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MqttValidation {
    public class MqttValidationTests {

        private const string EndpointKey = "Verifier_Endpoint";
        private const string BrokerEndpointKey = "Broker_Endpoint";
        private const string BrokerPortKey = "Broker_Port";
        private const string TopicKey = "Topic";
        private const string TimeToObserveKey = "Time_To_Observe";

        private swaggerClient? _swaggerClient;

        [SetUp]
        public void Setup() {
            string? endpoint = Environment.GetEnvironmentVariable(EndpointKey);
            if (string.IsNullOrWhiteSpace(endpoint)) {
                Assert.Ignore($"Environment variable '{EndpointKey}' not specified!");

            }

            HttpClient httpClient = new HttpClient();
            _swaggerClient = new swaggerClient(endpoint, httpClient);

        }

        [Test]
        public async Task SimpleVerification() {
            ArgumentNullException.ThrowIfNull(_swaggerClient);

            void SetFromEnv(Action<string> setter, string key) {
                string? value = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(value)) {
                    setter(value);
                }
            }

            var request = new MqttVerificationRequest();

            SetFromEnv(v => request.MqttBroker = v, BrokerEndpointKey);
            SetFromEnv(v => request.MqttPort = int.Parse(v), BrokerPortKey);
            SetFromEnv(v => request.MqttTopic = v, TopicKey);
            SetFromEnv(v => request.TimeToObserve = int.Parse(v), TimeToObserveKey);

            MqttVerificationResponse verificationTask = await _swaggerClient.StartVerificationAsync(request);


            MqttVerificationDetailedResponse result;

            do {
                result = await _swaggerClient.GetVerificationResultAsync(verificationTask.ValidationTaskId);

                if (result.IsFinished) {
                    break;
                }
                else {
                    await Task.Delay(1000);
                }
            } while (true);

            result.Error.Should().BeNullOrEmpty();
            result.NumberOfMessages.Should().BeGreaterThan(8);



        }
    }
}