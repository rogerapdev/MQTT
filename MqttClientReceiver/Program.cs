using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;

class Program
{
    private static readonly string ClientId = "So_recebe_mensagem";
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
                Console.WriteLine("Conectado ao servidor MQTT (Só recebe mensagem).");
                await SubscribeToTopic(mqttClient);
            };

            mqttClient.DisconnectedAsync += async e =>
            {

                Console.WriteLine("Desconectado do servidor MQTT (Só recebe mensagem).");
                if(ReconnetOnBroker)
                {
                    await ConnectBrocker(mqttClient, options);
                }
            };

            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine($"Mensagem recebida: Tópico = {e.ApplicationMessage.Topic}, Payload = {payload}");
            };

            await ConnectBrocker(mqttClient, options);

            do
            {
                // Verificação do estado da conexão antes de enviar uma mensagem
                if (mqttClient.IsConnected)
                {
                    Console.WriteLine("Conectado ao servidor MQTT. Aguardando mensagens... (Pressione ESC para sair)");
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
        Console.WriteLine("Inscrito aos tópicos: 'test/topic/#' e 'test/topic/status'.");
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
