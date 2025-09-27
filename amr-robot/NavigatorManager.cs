using System;
using System.Threading.Tasks;

namespace RobotApp
{
    public class NavigationManager
    {
        private readonly ActuatorManager _actuators;
        private readonly SensorManager _sensors;

        public NavigationManager(ActuatorManager actuators, SensorManager sensors)
        {
            _actuators = actuators;
            _sensors = sensors;
        }

        // =================== SENSORI ===================
        // Restituisce true se davanti c’è un ostacolo entro soglia
        public bool IsObstacleAhead(double sogliaMetri)
        {
            var distanza = _sensors.GetDistance();
            return distanza.HasValue && distanza.Value < sogliaMetri;
        }

        // Restituisce la direzione attuale in gradi (0-360) dal magnetometro
        public double GetHeadingDegrees() => _sensors.GetHeadingDegrees();

        // Guarda in una direzione specifica (in gradi)
        public async Task LookAtAsync(double relativeAngleDegrees)
        {
            // relativeAngleDegrees: -90 = sinistra, 0 = centro, 90 = destra
            _actuators.SetServoAngle(relativeAngleDegrees);

            // Attendi stabilizzazione del servo
            await Task.Delay(500);
        }

        // =================== MOVIMENTO ===================
        /// Ruota il robot di un certo angolo (in gradi) usando il magnetometro.
        public async Task RotateToAsync(double deltaDegrees, int tolerance)
        {
            // Heading attuale
            double startHeading = _sensors.GetHeadingDegrees();
            // Calcola angolo target normalizzato
            double targetHeading = NormalizeAngle(startHeading + deltaDegrees);

            // Determina direzione rotazione
            if (deltaDegrees > 0)
                _actuators.TurnRight();
            else
                _actuators.TurnLeft();

            //Ciclo di controllo fino a raggiungere l'angolo target
            while (true)
            {
                double currentHeading = _sensors.GetHeadingDegrees();
                double diff = AngleDifference(currentHeading, targetHeading);

                if (Math.Abs(diff) <= tolerance)
                    break;

                await Task.Delay(50); // controlla ogni 50ms
            }

            _actuators.StopMotors();
        }

        /// Muove il robot avanti di una certa distanza (in metri) usando stima tempo-velocità.
        public async Task MoveForwardAsync(double meters)
        {
            // speed = metri/secondo da calibrare --> 0.18 m/s ipotetico
            double seconds = meters / 0.18; // es. 1m = 5.50s

            _actuators.MoveForward();
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            _actuators.StopMotors();
        }

        public void StopMotors() => _actuators.Shutdown();


        // =================== METODI UTILI ===================

        // Normalizza un angolo tra 0 e 360 gradi
        public double NormalizeAngle(double angle)
        {
            angle %= 360;
            return angle < 0 ? angle + 360 : angle;
        }

        // Calcola la differenza angolare più piccola tra due angoli
        public double AngleDifference(double a, double b)
        {
            double diff = (a - b + 540) % 360 - 180;
            return diff;
        }

    }
}
