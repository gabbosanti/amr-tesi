using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RobotApp
{
    public class MQTTClient
    {
        private readonly string _broker;
        private readonly int _port;
        private readonly IMqttClient _client;

        // Evento pubblico che il Robot può usare
        public event Action<string, string>? MessageReceived;

        // Costruttore
        public MQTTClient(string broker, int port)
        {
            _broker = broker;
            _port = port;
            _client = new MqttFactory().CreateMqttClient();
        }

        // Connessione al broker
        public async Task Connect()
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_broker, _port)
                .WithClientId($"RobotApp_{Guid.NewGuid()}")
                .Build();

            _client.ConnectedAsync += e =>
            {
                Logger.Info($"Connesso al broker {_broker}:{_port}");
                return Task.CompletedTask;
            };

            _client.DisconnectedAsync += e =>
            {
                Logger.Warning("Disconnesso dal broker");
                return Task.CompletedTask;
            };

            _client.ApplicationMessageReceivedAsync += args =>
            {
                try
                {
                    string topic = args.ApplicationMessage?.Topic ?? "(nessun topic)";
                    string payload = args.ApplicationMessage?.Payload == null
                        ? string.Empty
                        : Encoding.UTF8.GetString(args.ApplicationMessage.Payload.ToArray());

                    Logger.Info($"[MQTT] Messaggio ricevuto sul topic '{topic}': {payload}");

                    // Solleva l’evento pubblico
                    MessageReceived?.Invoke(topic, payload);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error handling incoming MQTT message: {ex.Message}");
                }

                return Task.CompletedTask;
            };

            try
            {
                await _client.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                Logger.Error($"MQTT connection failed: {ex.Message}");
                throw;
            }
        }

        //Iscrizione a un topic
        public async Task Subscribe(string topic)
        {
            if (!_client.IsConnected)
            {
                Logger.Error("Cannot subscribe: client not connected.");
                return;
            }

            await _client.SubscribeAsync(topic);
            Logger.Info($"Subscribed to topic: {topic}");
        }

        // Pubblicazione di un messaggio
        public async Task Publish(string topic, string message)
        {
            if (!_client.IsConnected)
            {
                Logger.Error("Cannot publish: client not connected.");
                return;
            }

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce) // QoS 0
                .Build();

            await _client.PublishAsync(mqttMessage);
        }

        // Disconnessione
        public async Task Disconnect()
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
                Logger.Info("Disconnected cleanly from MQTT broker.");
            }
        }
        
    }
}
