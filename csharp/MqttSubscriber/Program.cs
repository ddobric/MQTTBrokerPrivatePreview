using Microsoft.Extensions.Hosting;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using MQTTnet;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MQTTnet.Server;

namespace MqttSubscriber
{
    internal class Program
    {
        private const string topic = "samples/#";
        static string host = "mqtt-sample0-RG-MQTT-BR-2d335f20.centraluseuap-1.ts.eventgrid.azure.net";

        static async Task Main(string[] args)
        {
            CancellationTokenSource tSrc = new CancellationTokenSource();

            Console.CancelKeyPress += (obj, arg) =>
            {
                Console.WriteLine("Cancel detected.");
                tSrc.Cancel();
            };

            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine("Mqtt Subscriber. Subscribes messages at the topic 'samples/#'");

            var caCert = X509Certificate.CreateFromCertFile(@"../../../../Certificates/azure-mqtt-test-only.root.ca.cert.pem");
            var clientCert = new X509Certificate2(@"../../../../Certificates/sub-client.cert.pfx", "1234");
            var certificates = new List<X509Certificate2>() { new X509Certificate2(caCert), new X509Certificate2(clientCert) };

            var mqttFactory = new MqttFactory();

            using (var mqttClient = new MqttFactory().CreateManagedMqttClient())
            {
                var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(30))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId("sub-client")
                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                    .WithTcpServer(host, 8883)
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

                mqttClient.ConnectedAsync += MqttClient_ConnectedAsync;

                mqttClient.ConnectedAsync += async (args) =>
                {
                    await mqttClient.SubscribeAsync(new List<MqttTopicFilter>()
                    {
                        new MqttTopicFilter()
                        {
                             QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                             Topic = topic
                        }
                    });
                };

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

                await mqttClient.StartAsync(options);

                Console.WriteLine("Waiting on messages...");

                tSrc.Token.WaitHandle.WaitOne();

                Console.WriteLine("...");

                // Dispose hangs!! Not required here. Used only to show hanging.
                // Known bug: https://github.com/dotnet/MQTTnet/issues/765
                mqttClient.Dispose();
            }

            Console.WriteLine("Exiting application...");
        }

        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static Task MqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            Console.WriteLine($"Connected: {arg.ConnectResult}");
            return Task.CompletedTask;
        }

        private static Task MqttClient_ConnectionStateChangedAsync(EventArgs arg)
        {
            Console.WriteLine($"Connection state changed: {arg}");
            return Task.CompletedTask;
        }

        private static Task MqttClient_ApplicationMessageProcessedAsync(ApplicationMessageProcessedEventArgs arg)
        {
            Console.WriteLine($"ApplicationMessageProcessed: {arg.ApplicationMessage}. Error: {arg.Exception}");
            return Task.CompletedTask;
        }

        private static Task MqttClient_ConnectingFailedAsync(ConnectingFailedEventArgs arg)
        {
            Console.WriteLine($"Connection failed: {arg.ConnectResult} - {arg.Exception}");
            return Task.CompletedTask;
        }
    }
}