
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IoTHubCredentialTools
{
    public class IoTHubRegistration
    {
        public const string _IoTHubAPIVersion = "?api-version=2016-11-14";

        /// <summary>
        /// Returns an array of the parsed parts of a connection string
        /// </summary>
        public static string[] ParseConnectionString(string connectionString, bool isDevice)
        {
            string[] connectionStringParts = connectionString.Split(';');
            if (connectionStringParts.Length == 3)
            {
                if (connectionStringParts[0].StartsWith("HostName="))
                {
                    connectionStringParts[0] = connectionStringParts[0].Substring(connectionStringParts[0].IndexOf('=') + 1);
                }
                else
                {
                    return null;
                }

                if (connectionStringParts[1].StartsWith("DeviceId=") && (isDevice == true))
                {
                    connectionStringParts[1] = connectionStringParts[1].Substring(connectionStringParts[1].IndexOf('=') + 1);
                }
                else if (connectionStringParts[1].StartsWith("SharedAccessKeyName=") && (isDevice == false))
                {
                    connectionStringParts[1] = connectionStringParts[1].Substring(connectionStringParts[1].IndexOf('=') + 1);
                }
                else
                {
                    return null;
                }

                if (connectionStringParts[2].StartsWith("SharedAccessKey="))
                {
                    connectionStringParts[2] = connectionStringParts[2].Substring(connectionStringParts[2].IndexOf('=') + 1);
                }
                else
                {
                    return null;
                }

                return connectionStringParts;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a device in the IoT Hub device registry using the IoT Hub REST API
        /// </summary>
        public static async Task<string> CreateDeviceInIoTHubDeviceRegistry(HttpClient httpClient, string deviceName)
        {
            // check if device already registered
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/devices/" + deviceName + _IoTHubAPIVersion);

            HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                // already registered, delete existing device first
                request = new HttpRequestMessage(HttpMethod.Delete, "/devices/" + deviceName + _IoTHubAPIVersion);
                request.Headers.IfMatch.Add(new EntityTagHeaderValue("\"*\""));

                response = await httpClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Delete device failed with " + response.Content.ReadAsStringAsync().Result);
                }
            }

            // now create a new one
            string jsonMessage = "{\"deviceId\": \"" + deviceName + "\"}";
            request = new HttpRequestMessage(HttpMethod.Put, "/devices/" + deviceName + _IoTHubAPIVersion)
            {
                Content = new StringContent(jsonMessage, Encoding.ASCII, "application/json")
            };

            response = await httpClient.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Create device failed with " + response.Content.ReadAsStringAsync().Result);
            }

            string result = response.Content.ReadAsStringAsync().Result;
            if (result.Contains("primaryKey"))
            {
                const string keyIdentifier = "\"primaryKey\":\"";
                const int keylength = 44;
                return result.Substring(result.IndexOf(keyIdentifier) + keyIdentifier.Length, keylength);
            }
            else
            {
                throw new Exception("Could not find primary key in response: " + response.Content.ReadAsStringAsync().Result);
            }
        }

        /// <summary>
        /// Registers a device with IoT Hub
        /// </summary>
        public static string RegisterDeviceWithIoTHub(string deviceName, string IoTHubOwnerConnectionString)
        {
            string[] parsedConnectionString = ParseConnectionString(IoTHubOwnerConnectionString, false);
            string deviceConnectionString = string.Empty;
            if ((parsedConnectionString != null) && (parsedConnectionString.Length == 3))
            {
                string IoTHubName = parsedConnectionString[0];
                string name = parsedConnectionString[1];
                string accessToken = parsedConnectionString[2];

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new UriBuilder { Scheme = "https", Host = IoTHubName }.Uri;

                    string sharedAccessSignature = GenerateSharedAccessToken(name, Convert.FromBase64String(accessToken), IoTHubName, 60000);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SharedAccessSignature", sharedAccessSignature);
                    deviceConnectionString = CreateDeviceInIoTHubDeviceRegistry(httpClient, deviceName.Replace(" ", "")).Result;

                    // prepend the rest of the connection string
                    deviceConnectionString = "HostName=" + IoTHubName + ";DeviceId=" + deviceName.Replace(" ", "") + ";SharedAccessKey=" + deviceConnectionString;
                    return deviceConnectionString;
                }
            }
            else
            {
                throw new Exception("Could not parse IoT Hub owner connection string: " + IoTHubOwnerConnectionString);
            }
        }

        /// <summary>
        /// Sas token generation
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="key"></param>
        /// <param name="tokenScope"></param>
        /// <param name="ttl"></param>
        /// <returns>shared access token</returns>
        public static string GenerateSharedAccessToken(string keyName, byte[] key, string tokenScope, int ttl)
        {
            // http://msdn.microsoft.com/en-us/library/azure/dn170477.aspx
            // signature is computed from joined encoded request Uri string and expiry string

            DateTime expiryTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(ttl);
            string expiry = ((long)(expiryTime - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            string encodedScope = Uri.EscapeDataString(tokenScope);
            string sig;

            // the connection string signature is base64 encoded
            using (var hmac = new HMACSHA256(key))
            {
                sig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedScope + "\n" + expiry)));
            }

            return string.Format(
                "sr={0}&sig={1}&se={2}&skn={3}",
                encodedScope,
                Uri.EscapeDataString(sig),
                Uri.EscapeDataString(expiry),
                Uri.EscapeDataString(keyName)
                );
        }
    }
}
