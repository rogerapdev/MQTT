using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;

class Program
{
    private static readonly string ClientId = "Envia_recebe_mensagem";
    private static readonly string MessageIdentifier = Guid.NewGuid().ToString();
    private static bool ReconnetOnBroker = true;

    static async Task Main(string[] args)
    {
        Console.SetWindowSize(130, 10);

        var factory = new MqttFactory();

        using (var mqttClient = factory.CreateMqttClient())
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(ClientId)
                .WithTcpServer("localhost", 1883) // Endereço e porta do servidor MQTT TCP
                .WithCleanSession()
                //Configurações do LWT
                .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithWillTopic("test/topic/status")
                .WithWillPayload($"{ClientId} está offline") // Definindo a mensagem LWT
                .WithWillRetain(true)
                .Build();


            mqttClient.ConnectedAsync += async e =>
            {
                Console.WriteLine("Conectado ao servidor MQTT (Envia e recebe mensagem).");
                await SubscribeToTopic(mqttClient);
            };

            mqttClient.DisconnectedAsync += async e =>
            {
                Console.WriteLine("Desconectado do servidor MQTT (Envia e recebe mensagem).");
                if (ReconnetOnBroker)
                {
                    await ConnectBrocker(mqttClient, options);
                }
            };

            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                if (!payload.Contains(MessageIdentifier))
                {
                    Console.WriteLine($"Mensagem recebida: Tópico = {e.ApplicationMessage.Topic}, Payload = {payload}");
                }
            };

            await mqttClient.ConnectAsync(options, CancellationToken.None);

            do
            {
                // Verificação do estado da conexão antes de enviar uma mensagem
                if (mqttClient.IsConnected)
                {

                    Console.WriteLine("Conectado ao servidor MQTT. Aguardando mensagens... (Pressione ESC para sair)");
                    Console.ReadLine();

                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("test/topic")
                        .WithPayload($"Olá do cliente MQTT (Envia e recebe mensagem)! [{MessageIdentifier}]")
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                        .WithRetainFlag(true)
                        .Build();

                    await mqttClient.PublishAsync(message);

                    Console.WriteLine("Mensagem publicada. Pressione Enter para sair.");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("Não foi possível enviar a mensagem. O cliente não está conectado ao servidor.");
                    await ConnectBrocker(mqttClient, options);
                }

            } while (Console.ReadKey().Key != ConsoleKey.Escape);

            ReconnetOnBroker = false;

            await mqttClient.DisconnectAsync();

        }

    }

    private static async Task SubscribeToTopic(IMqttClient mqttClient)
    {
        await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("test/topic/#").Build());
        Console.WriteLine("Inscrito no tópico 'test/topic/#'.");
    }

    private static async Task ConnectBrocker(IMqttClient mqttClient, MqttClientOptions options)
    {
        Console.WriteLine("Aguarde conectando ao Servidor....");
        do
        {
            Task.Delay(50).GetAwaiter().GetResult();

            await mqttClient.ConnectAsync(options, CancellationToken.None);

        } while (!mqttClient.IsConnected);
    }
}
