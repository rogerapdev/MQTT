using MQTTnet;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Text;
using System.Text.Json;
using MQTTnet.Packets;

class Program
{
    static async Task Main(string[] args)
    {

        Console.SetWindowSize(130, 10);

        // Caminho do arquivo onde as mensagens retidas serão armazenadas.
        //var storePath = Path.Combine(Path.GetTempPath(), "RetainedMessages.json");

        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(1883) //porta do servidor MQTT
            .Build(); 

        using (var mqttServer = new MqttFactory().CreateMqttServer(optionsBuilder))
        {
            //Configura a validação da conexão antes de iniciar o servidor para que não haja nenhuma alteração na conexão sem credenciais válidas.
            mqttServer.ValidatingConnectionAsync += context =>
            {

                if (context.ClientId.Contains("So_envia_mensagem") && (context.Password != "123456" || context.Username != "root"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[ Conexão do cliente : {context.ClientId} foi recusada devido a informações de autenticação inválidas ]");
                    Console.WriteLine($"[ Endpoint : {context.Endpoint} ]");
                    Console.ForegroundColor = ConsoleColor.White;

                    context.ReasonCode = MqttConnectReasonCode.NotAuthorized;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[ Novo Cliente : {context.ClientId} conectou-se com sucesso ]");
                    Console.WriteLine($"[ Endpoint : {context.Endpoint} ]");
                    Console.ForegroundColor = ConsoleColor.White;

                    context.ReasonCode = MqttConnectReasonCode.Success;
                }

                return Task.CompletedTask;
            };

            mqttServer.InterceptingPublishAsync += context =>
            {
                Console.WriteLine($"Nova mensagem: ClientId = {context.ClientId}, Tópico = {context.ApplicationMessage.Topic}, Payload = {Encoding.UTF8.GetString(context.ApplicationMessage.Payload)}, QoS = {context.ApplicationMessage.QualityOfServiceLevel}, Retain-Flag = {context.ApplicationMessage.Retain}");
                return CompletedTask.Instance;
            };

            mqttServer.InterceptingSubscriptionAsync += context =>
            {
                Console.WriteLine($"Nova subscrição: ClientId = {context.ClientId}, Tópico = {context.TopicFilter.Topic}");
                return CompletedTask.Instance;
            };

            mqttServer.ClientConnectedAsync += async context =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Cliente conectado: '{context.ClientId}");
                Console.ForegroundColor = ConsoleColor.White;
                await Task.CompletedTask;
            };

            mqttServer.ClientDisconnectedAsync += async context =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Cliente desconectado: {context.ClientId}");
                Console.ForegroundColor = ConsoleColor.White;
                await Task.CompletedTask;
            };

            mqttServer.StartedAsync += async context =>
            {
                Console.WriteLine("Servidor MQTT iniciado na porta 1883.");
                await Task.CompletedTask;
            };

            mqttServer.StoppedAsync += async context =>
            {
                Console.WriteLine("Servidor MQTT encerrado.");
                await Task.CompletedTask;
            };


            //// Evento para carregar mensagens retidas ao iniciar o servidor.
            //mqttServer.LoadingRetainedMessageAsync += async eventArgs =>
            //{
            //    try
            //    {
            //        // Deserializa as mensagens retidas a partir do arquivo JSON.
            //        var models = await JsonSerializer.DeserializeAsync<List<MqttRetainedMessageModel>>(File.OpenRead(storePath)) ?? new List<MqttRetainedMessageModel>();
            //        var retainedMessages = models.Select(m => m.ToApplicationMessage()).ToList();

            //        eventArgs.LoadedRetainedMessages = retainedMessages;  // Carrega as mensagens retidas no evento.
            //        Console.WriteLine("Mensagens retidas carregadas.");
            //        File.Delete(storePath);
            //    }
            //    catch (FileNotFoundException)
            //    {
            //        // Ignora caso o arquivo não exista, pois nenhuma mensagem foi armazenada ainda.
            //        Console.WriteLine("Nenhuma mensagem retida armazenada ainda.");
            //    }
            //    catch (Exception exception)
            //    {
            //        // Tratamento genérico de exceções.
            //        Console.WriteLine(exception);
            //    }
            //};

            //// Evento para salvar mensagens retidas quando elas são alteradas.
            //mqttServer.RetainedMessageChangedAsync += async eventArgs =>
            //{
            //    try
            //    {
            //        // Converte as mensagens retidas em um formato serializável.
            //        var models = eventArgs.StoredRetainedMessages.Select(MqttRetainedMessageModel.Create);

            //        // Serializa as mensagens retidas para um buffer de bytes e salva no arquivo.
            //        var buffer = JsonSerializer.SerializeToUtf8Bytes(models);
            //        await File.WriteAllBytesAsync(storePath, buffer);
            //        Console.WriteLine("Mensagens retidas salvas.");
            //    }
            //    catch (Exception exception)
            //    {
            //        // Tratamento genérico de exceções.
            //        Console.WriteLine(exception);
            //    }
            //};

            //// Evento para limpar as mensagens retidas quando elas são todas excluídas via API.
            //mqttServer.RetainedMessagesClearedAsync += _ =>
            //{
            //    File.Delete(storePath);  // Deleta o arquivo que contém as mensagens retidas.
            //    return Task.CompletedTask;
            //};

            await mqttServer.StartAsync();

            Console.WriteLine("Pressione Enter para encerrar.");
            Console.ReadLine();

            await mqttServer.StopAsync();
        }

    }

    // Classe para representar o modelo de uma mensagem retida, facilitando a serialização e desserialização.
    sealed class MqttRetainedMessageModel
    {
        public string? ContentType { get; set; }  // Tipo de conteúdo da mensagem.
        public byte[]? CorrelationData { get; set; }  // Dados de correlação para identificar a mensagem.
        public byte[]? Payload { get; set; }  // Dados da mensagem.
        public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }  // Indicador do formato do payload.
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }  // Nível de qualidade de serviço da mensagem.
        public string? ResponseTopic { get; set; }  // Tópico de resposta para a mensagem.
        public string? Topic { get; set; }  // Tópico ao qual a mensagem pertence.
        public List<MqttUserProperty>? UserProperties { get; set; }  // Propriedades adicionais definidas pelo usuário.

        // Método para criar um modelo de mensagem retida a partir de uma mensagem de aplicação MQTT.
        public static MqttRetainedMessageModel Create(MqttApplicationMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));  // Verifica se a mensagem é nula.
            }

            return new MqttRetainedMessageModel
            {
                Topic = message.Topic,

                // Cria uma cópia do buffer do segmento de payload porque não pode ser serializado diretamente.
                Payload = message.PayloadSegment.ToArray(),
                UserProperties = message.UserProperties,
                ResponseTopic = message.ResponseTopic,
                CorrelationData = message.CorrelationData,
                ContentType = message.ContentType,
                PayloadFormatIndicator = message.PayloadFormatIndicator,
                QualityOfServiceLevel = message.QualityOfServiceLevel

                // Outras propriedades, como "Retain", não são de interesse no armazenamento.
            };
        }

        // Método para converter o modelo de mensagem retida de volta para uma mensagem de aplicação MQTT.
        public MqttApplicationMessage ToApplicationMessage()
        {
            return new MqttApplicationMessage
            {
                Topic = Topic,
                PayloadSegment = new ArraySegment<byte>(Payload ?? Array.Empty<byte>()),
                PayloadFormatIndicator = PayloadFormatIndicator,
                ResponseTopic = ResponseTopic,
                CorrelationData = CorrelationData,
                ContentType = ContentType,
                UserProperties = UserProperties,
                QualityOfServiceLevel = QualityOfServiceLevel,
                Dup = false,  // Indica que a mensagem não é uma duplicata.
                Retain = true  // Indica que a mensagem deve ser retida.
            };
        }
    }

}
