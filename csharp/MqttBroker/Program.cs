using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MqttBroker
{
    // https://github.com/dotnet/MQTTnet/wiki/Client
    internal class Program
    {
        static string host = "mqtt-sample0-RG-MQTT-BR-2d335f20.centraluseuap-1.ts.eventgrid.azure.net";

        /// <summary>
        /// Demonstrates how to use the same client to publish and subscribe messages.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {

            Console.WriteLine("Hello, MQTT Broker!");

            var port = 8885;
            port = 1883;
            port = 8883;
            // Certificate Formats: https://thesecmaster.com/what-are-the-different-types-of-certificate-formats/

            var caCert = X509Certificate.CreateFromCertFile(@"../../../../Certificates/azure-mqtt-test-only.root.ca.cert.pem");
            var clientCert = new X509Certificate2(@"../../../../Certificates/pub-client.cert.pfx", "1234");
            var certificates = new List<X509Certificate2>() { new X509Certificate2(caCert), new X509Certificate2(clientCert) };
       
            var mqttFactory = new MqttFactory();

            using (var mqttClient = new MqttFactory().CreateManagedMqttClient())
            {
                var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(30))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId("pub-client")
                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                    .WithTcpServer(host, port)
                    // Alternatively user credentials can be used.
                    //.WithCredentials(username, psw)
                    .WithTls(new MqttClientOptionsBuilderTlsParameters()
                    {
                        AllowUntrustedCertificates = true,
                        UseTls = true,
                        Certificates = certificates,
                        CertificateValidationHandler = delegate { return true; },
                        IgnoreCertificateChainErrors = true,
                        IgnoreCertificateRevocationErrors = true
                    })
                    .WithCleanSession()
                    .Build())
                .Build();

                mqttClient.ConnectingFailedAsync += MqttClient_ConnectingFailedAsync;
                mqttClient.ConnectionStateChangedAsync += MqttClient_ConnectionStateChangedAsync;
                mqttClient.ApplicationMessageProcessedAsync += MqttClient_ApplicationMessageProcessedAsync;

                mqttClient.ConnectedAsync += (args) =>
                {
                    _ = Task.Run(async () =>
                    {
                        while (true)
                        {
                            var applicationMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("samples/topic")
                            .WithPayload(new Random().Next(15, 40).ToString())
                            .Build();

                            await mqttClient.EnqueueAsync(new ManagedMqttApplicationMessage()
                            {
                                Id = Guid.NewGuid(),
                                ApplicationMessage = applicationMessage
                            });

                            Console.WriteLine("MQTT application message is published.");

                            Console.WriteLine("Press any key to send a next message...");

                            Console.ReadLine();
                        }
                    });

                    return Task.CompletedTask;
                };

                await mqttClient.StartAsync(options);

                await mqttClient.SubscribeAsync(new List<MqttTopicFilter>() 
                { 
                    new MqttTopicFilter()
                    {
                         QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                          Topic = "samples/#"
                    } });

                    mqttClient.ApplicationMessageReceivedAsync += (msg) =>
                    {
                        try
                        {
                            string topic = msg.ApplicationMessage.Topic;

                            if (string.IsNullOrWhiteSpace(topic) == false)
                            {
                                string payload = Encoding.UTF8.GetString(msg.ApplicationMessage.Payload);
                                Console.WriteLine($"Topic: {topic}. Message Received: {payload}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message, ex);
                        }

                        return Task.CompletedTask;
                    };

                await Task.Delay(int.MaxValue);
            }

        }

        private static Task MqttClient_ConnectionStateChangedAsync(EventArgs arg)
        {
            return Task.CompletedTask;
        }

        private static Task MqttClient_ApplicationMessageProcessedAsync(ApplicationMessageProcessedEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private static Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private static Task MqttClient_ConnectingFailedAsync(ConnectingFailedEventArgs arg)
        {
            return Task.CompletedTask;
        }
    }
}