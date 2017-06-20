// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IoTHubCredentialTools;
using Microsoft.Azure.Devices.Gateway;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Opc.Ua.Publisher
{
    /// <summary>
    /// Gateway module that acts as IoT Hub connectivity
    /// </summary>
    public class Module : IGatewayModule
    {
        private static AmqpConnection m_publisher = new AmqpConnection();
        private static TraceConfiguration m_trace = new TraceConfiguration();

        /// <summary>
        /// Trace message helper
        /// </summary>
        public static void Trace(string message)
        {
            Utils.Trace(message);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Create module, throws if configuration is bad
        /// </summary>
        public void Create(Broker broker, byte[] configuration)
        {
            string appName = Encoding.UTF8.GetString(configuration);

            // enable logging
            m_trace.DeleteOnLoad = true;
            m_trace.TraceMasks = 519;
            m_trace.OutputFilePath = "./Logs/" + appName + ".IoTHub.Module.log.txt";
            m_trace.ApplySettings();

            Trace("Opc.Ua.IoTHub.Module: Creating...");

            // configure connection
            try
            {
                ConfigureAMQPConnectionToIoTHub(appName).Wait();
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "Failed to configure AMQP connection, dropping....");
            }

            Trace("Opc.Ua.IoTHub.Module: Created.");
        }

        /// <summary>
        /// Disconnect all sessions
        /// </summary>
        public void Destroy()
        {
            m_publisher.Close();

            Trace("Opc.Ua.IoTHub.Module: Closed.");
        }

        /// <summary>
        /// Receive message from broker
        /// </summary>
        public void Receive(Message received_message)
        {
            try
            {
                if (!m_publisher.IsClosed())
                {
                    m_publisher.Publish(new ArraySegment<byte>(received_message.Content));
                    Utils.Trace("Published message for device " + received_message.Properties["deviceName"]);
                }
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "Failed to publish message, dropping....");
            }
        }

        /// <summary>
        /// Publish message to bus
        /// </summary>
        public static void Publish(Message message)
        {
            // NO-OP
        }

        /// <summary>
        /// Configures the AMQP (telemetry) connection to IoT Hub 
        /// </summary>
        public static async Task ConfigureAMQPConnectionToIoTHub(string appName)
        {
            Trace("Opc.Ua.IoTHub.Module: Attemping to read connection string from secure store with certificate name: " + appName);
            string connectionString = SecureIoTHubToken.Read(appName);

            Trace("Opc.Ua.IoTHub.Module: Attemping to configure IoTHub module with connection string: " + connectionString);
            string[] parsedConnectionString = IoTHubRegistration.ParseConnectionString(connectionString, true);
            string IoTHubName = parsedConnectionString[0];
            string deviceName = parsedConnectionString[1];
            string accessToken = parsedConnectionString[2];

            m_publisher.Endpoint = "/devices/" + deviceName + "/messages/events";
            m_publisher.WebSocketEndpoint = null; // not used
            m_publisher.Host = IoTHubName;
            m_publisher.Port = 0; // use default
            m_publisher.KeyName = ""; // not used
            m_publisher.KeyValue = accessToken;
            m_publisher.KeyEncoding = "Base64";
            m_publisher.UseCbs = true;
            m_publisher.TokenType = "servicebus.windows.net:sastoken";
            m_publisher.TokenScope = m_publisher.Host + "/devices/" + deviceName;
            m_publisher.TokenLifetime = 60000;

            await m_publisher.OpenAsync();
        }
    }
}
