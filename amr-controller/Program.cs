using System;
using System.Threading.Tasks;

namespace ControllerApp
{
    class Program
    {
        public const string IP_BROKER = "localhost";
        public const int PORT_BROKER = 1883;

        static async Task Main(string[] args)
        {

            Logger.Info("===Avvio applicazione Controller===");

            // Setup MQTT
            var mqttClient = new MQTTClient(IP_BROKER, PORT_BROKER);
            await mqttClient.ConnectAsync();

            // Sottoscrivo al topic del robot
            await mqttClient.SubscribeAsync("amr/status");

            Logger.Info("Inserisci il punto di destinazione ('exit' per uscire):");

            while (true)
            {
                string? input = Console.ReadLine();
                if (input?.ToLower() == "exit") break;

                if (!string.IsNullOrWhiteSpace(input))
                {
                    await mqttClient.PublishAsync("amr/command", input);
                    Logger.Info($"Invio comando : {input}");
                }
            }

            Logger.Info("Disconnecting...");
            await mqttClient.DisconnectAsync();
        }
    }

}
