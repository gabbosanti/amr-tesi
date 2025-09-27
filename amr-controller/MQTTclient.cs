using MQTTnet;
using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;

namespace ControllerApp
{
    public class MQTTClient
    {
        private readonly string _broker;
        private readonly int _port;
        private IMqttClient _client;

        //Costruttore
        public MQTTClient(string broker, int port)
        {
            _broker = broker;
            _port = port;
            _client = new MqttClientFactory().CreateMqttClient();
        }

        public async Task ConnectAsync() {

            //Costruisco le opzioni di connessione
            var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_broker, _port)
            .WithClientId("ControllerApp_" + Guid.NewGuid())
            .Build();

            //Handler --> connessione eseguita con successo
            _client.ConnectedAsync += e =>
            {
                Logger.Info($"Connesso al broker {_broker}:{_port}");
                return Task.CompletedTask;
            };

            //Handler --> disconnessione
            _client.DisconnectedAsync += e =>
            {
                Logger.Warning("Disconnesso dal broker.");
                return Task.CompletedTask;
            };

            //Handler --> messaggio ricevuto sui topic a cui sono iscritto
            _client.ApplicationMessageReceivedAsync += async args =>
            {
                try {
                    var appMsg = args.ApplicationMessage;
                    string topic = appMsg?.Topic ?? "(nessun topic)";

                    // Se Payload è null, usiamo una stringa vuota
                    string payload = string.Empty;

                    if (appMsg?.Payload != null)
                    {
                        payload = Encoding.UTF8.GetString(appMsg.Payload.ToArray());
                        Logger.Info($"[MQTT] Messaggio ricevuto sul topic '{topic}': {payload}");
                    }
                } catch (Exception ex) {
                    Logger.Error($"Error handling incoming MQTT message: {ex.Message}");
                }

                await Task.CompletedTask;
            };

            try
            {

                await _client.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                Logger.Error($"Errore sul messaggio MQTT in entrata : {ex.Message}");
                throw;
            }
        }
    
        public async Task SubscribeAsync(string topic) {

            if (!_client.IsConnected)
            {
                Logger.Error("Impossibile iscriversi : client non connesso.");
                return;
            }

            await _client.SubscribeAsync(topic);
            Logger.Info($"Iscritto al topic: {topic}");
        }

        public async Task PublishAsync(string topic, string message) {
            if (!_client.IsConnected)
            {
                Logger.Error("Impossibile pubblicare: client non connesso.");
                return;
            }

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(0)
                .Build();

            await _client.PublishAsync(mqttMessage);
        }

        public async Task DisconnectAsync()
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
                Logger.Info("Disconnesso con successo dal broker MQTT.");
            }
        }
    }
}
