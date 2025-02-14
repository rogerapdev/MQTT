using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

class Program
{
    private static readonly string ClientId = "So_envia_mensagem";
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
                .WithCredentials("root", "123456")
                .WithCleanSession()
                //Configurações do LWT
                .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithWillTopic("test/topic/status")
                .WithWillPayload($"{ClientId} está offline") // Definindo a mensagem LWT
                .WithWillRetain(true)
                .Build();


            mqttClient.ConnectedAsync += async e =>
            {
                Console.WriteLine("Conectado ao servidor MQTT (Só envia mensagem).");
            };

            mqttClient.DisconnectedAsync += async e =>
            {
                Console.WriteLine("Desconectado do servidor MQTT (Só envia mensagem).");
                if (ReconnetOnBroker)
                {
                    await ConnectBrocker(mqttClient, options);
                }
            };


            await ConnectBrocker(mqttClient, options);

            Console.WriteLine("Conectado ao servidor MQTT. Pressione Enter para iniciar envio das mensagens");
            Console.ReadLine();

            int numeroMsg = 1;
            do
            {
                // Verificação do estado da conexão antes de enviar uma mensagem
                if (mqttClient.IsConnected)
                {

                    var message = new MqttApplicationMessageBuilder()
                   .WithTopic("test/topic")
                   .WithPayload($"Olá do cliente MQTT: msg {numeroMsg} (Só envia mensagem)!")
                   .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                   .WithRetainFlag(true)
                   .Build();

                    await mqttClient.PublishAsync(message);

                    Console.WriteLine("Mensagem publicada. Pressione Enter para enviar nova mensagem. (Pressione ESC para sair) ");
                    numeroMsg++;

                    //Escape = Console.ReadKey().Key;

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

