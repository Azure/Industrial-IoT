// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IoTHubCredentialTools;
using Microsoft.Azure.Devices.Gateway;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Opc.Ua.IoTHub
{
    /// <summary>
    /// Gateway module that acts as IoT Hub connectivity
    /// </summary>
    public class Module : IGatewayModule
    {
        private static AmqpConnection m_publisher = new AmqpConnection();
        private static StreamWriter m_trace = null;

        /// <summary>
        /// Trace message helper
        /// </summary>
        public static void Trace(string message, params object[] args)
        {
            m_trace.WriteLine(message, args);
            m_trace.Flush();
            Console.WriteLine(message, args);
        }

        public static void Trace(int traceMask, string format, params object[] args)
        {
            m_trace.WriteLine(format, args);
            m_trace.Flush();
            Console.WriteLine(format, args);
        }

        public static void Trace(Exception e, string format, params object[] args)
        {
            m_trace.WriteLine(e.ToString());
            m_trace.WriteLine(format, args);
            m_trace.Flush();
            Console.WriteLine(e.ToString());
            Console.WriteLine(format, args);
        }

        /// <summary>
        /// Create module, throws if configuration is bad
        /// </summary>
        public void Create(Broker broker, byte[] configuration)
        {
            string appName = Encoding.UTF8.GetString(configuration).Replace("\"","");

            // enable logging
            string logpath = "./Logs/" + appName + ".IoTHub.Module.log.txt";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_LOGP")))
            {
                logpath = Environment.GetEnvironmentVariable("_GW_LOGP").Replace(".txt", ".IoTHub.Module.txt");
            }
            m_trace = new StreamWriter(File.Open(logpath, FileMode.Create, FileAccess.Write, FileShare.Read));
           
            Trace("Opc.Ua.IoTHub.Module: Creating...");

            // configure connection
            try
            {
                ConfigureAMQPConnectionToIoTHub(appName).Wait();
            }
            catch (Exception ex)
            {
                Module.Trace(ex, "Failed to configure AMQP connection, dropping....");
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

            m_trace.Flush();
            m_trace.Dispose();
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
                }
            }
            catch (Exception ex)
            {
                Module.Trace(ex, "Failed to publish message, dropping....");
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
