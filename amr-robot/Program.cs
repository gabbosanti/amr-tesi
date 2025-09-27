using System;
using System.Threading.Tasks;

namespace RobotApp
{
    class Program
    {
        public const string IP_BROKER = "192.168.1.50";
        public const int PORT_BROKER = 1883;

        static async Task Main()
        {
            Console.WriteLine("Avvio RobotApp...");

            // ======= SENSORI =======
            SensorManager sensorManager = new SensorManager();

            // ======= ATTUATORI =======
            ActuatorManager actuatorManager = new ActuatorManager(); 

            NavigationManager navigationManager = new NavigationManager(actuatorManager, sensorManager);

            // ======= ALGORITMO =======
            INavigationAlgorithm bug2 = new Bug2Algorithm(navigationManager);

            // ======= MQTT =======
            MQTTClient mqttClient = new MQTTClient(IP_BROKER, PORT_BROKER);
            await mqttClient.Connect(); //Connessione al broker
            
            Robot robot = new Robot(mqttClient, bug2);

            // Connetti e iscriviti ai comandi
            await robot.StartAsync();

            Console.WriteLine("Robot pronto e in ascolto dei comandi...");

            // Mantieni il programma in esecuzione
            await Task.Delay(-1);
        }
    }
}
