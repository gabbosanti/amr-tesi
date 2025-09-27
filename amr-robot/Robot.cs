using System;
using System.Threading.Tasks;

namespace RobotApp
{
    public class Robot
    {
        private readonly MQTTClient _mqttClient;
        private readonly INavigationAlgorithm _algorithm;

        public Robot(MQTTClient mqttClient, INavigationAlgorithm algorithm)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));

            _mqttClient.MessageReceived += OnMqttMessageReceived;
            _algorithm.ObstacleDetected += OnObstacleDetected;
            _algorithm.GoalReached += OnGoalReached;
        }

        // Handler : ostacolo rilevato dall'algoritmo
        private void OnObstacleDetected() {  _ = _mqttClient.Publish("amr/status", "OBSTACLE"); }

        // Handler : obiettivo raggiunto dall'algoritmo
        private void OnGoalReached(){ _ = _mqttClient.Publish("amr/status", "GOAL_REACHED");}

        /// Handler : messaggi MQTT ricevuti
        private void OnMqttMessageReceived(string topic, string payload)
        {
            Logger.Info($"[Robot] Command received on {topic}: {payload}");

            switch (payload.ToUpperInvariant())
            {
                case "START":
                    _ = _algorithm.StartNavigationAsync()
                        .ContinueWith(t =>
                        {
                            if (t.Exception != null)
                                Logger.Error(t.Exception.ToString());
                        });
                    _ = _mqttClient.Publish("amr/status", "MOVING");
                    break;

                case "STOP":
                    _algorithm.StopNavigation();
                    _ = _mqttClient.Publish("amr/status", "STOPPED");
                    break;

                default:
                    Logger.Warning($"[Robot] Unknown command: {payload}");
                    break;
            }
        }

        /// Avvio del robot: subscribe topic comandi e stato iniziale
        public async Task StartAsync()
        {
            await _mqttClient.Subscribe("amr/command");
            await _mqttClient.Publish("amr/status", "READY");
            Logger.Info("[Robot] Robot pronto e in ascolto dei comandi.");
        }
    }
}
