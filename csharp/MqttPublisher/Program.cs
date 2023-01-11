using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MqttPublisher
{
    internal class Program
    {
        static string host = "mqtt-sample0-RG-MQTT-BR-2d335f20.centraluseuap-1.ts.eventgrid.azure.net";

        static async Task Main(string[] args)
        {
            CancellationTokenSource tSrc = new CancellationTokenSource();

            Console.CancelKeyPress += (obj, arg) =>
            {
                Console.WriteLine("Cancel detected.");
                tSrc.Cancel();
            };

            Console.ForegroundColor= ConsoleColor.Green;
            Console.WriteLine("MQTT Publisher!");

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
                    .WithTcpServer(host, 8883)
                    .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
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
                            Console.WriteLine("Please enter the message text");

                            string? userTxt = Console.ReadLine();

                            var applicationMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("samples/topic")
                            .WithPayload(userTxt)
                            .Build();

                            await mqttClient.EnqueueAsync(new ManagedMqttApplicationMessage()
                            {
                                Id = Guid.NewGuid(),
                                ApplicationMessage = applicationMessage
                            });

                            Console.WriteLine("MQTT application message is published.");
                        }
                    });

                    return Task.CompletedTask;
                };

                await mqttClient.StartAsync(options);

                   tSrc.Token.WaitHandle.WaitOne();

                Console.WriteLine("...");

                // Dispose hangs!! Not required here. Used only to show hanging.
                // Known bug: https://github.com/dotnet/MQTTnet/issues/765
                mqttClient.Dispose();
            }

            Console.WriteLine("Exiting application...");                
        }

        private static Task MqttClient_ConnectionStateChangedAsync(EventArgs arg)
        {
            return Task.CompletedTask;
        }

        private static Task MqttClient_ApplicationMessageProcessedAsync(ApplicationMessageProcessedEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private static Task MqttClient_ConnectingFailedAsync(ConnectingFailedEventArgs arg)
        {
            return Task.CompletedTask;
        }
    }
}