using System;
using System.Device.Gpio;
using System.Device.Pwm;
using System.Threading.Tasks;

namespace RobotApp
{
    public sealed class MotorController : IDisposable
    {
        private readonly GpioController _gpio;
        private readonly PwmChannel _pwmLeft;
        private readonly PwmChannel _pwmRight;

        private readonly int _leftA, _leftB, _rightA, _rightB;
        private const double DefaultSpeed = 0.5; 
        private const double KickStartBoost_left = 0.2; // duty aggiuntivo iniziale sinistro
        private const double KickStartBoost_rigth = 0.22; // duty aggiuntivo iniziale destro
        private const int KickStartDurationMs = 150; // durata della "spinta" iniziale

        public MotorController(int leftA, int leftB, int rightA, int rightB)
        {
            _gpio = new GpioController(); 

            _leftA = leftA;
            _leftB = leftB;
            _rightA = rightA;
            _rightB = rightB;

            _gpio.OpenPin(_leftA, PinMode.Output);
            _gpio.OpenPin(_leftB, PinMode.Output);
            _gpio.OpenPin(_rightA, PinMode.Output);
            _gpio.OpenPin(_rightB, PinMode.Output);

            // PWM hardware sui pin supportati (GPIO18 = PWM0, GPIO19 = PWM1)
            _pwmLeft = PwmChannel.Create(chip: 0, channel: 0, frequency: 1000);
            _pwmRight = PwmChannel.Create(chip: 0, channel: 1, frequency: 1000);

            // Avvia i canali PWM
            _pwmLeft.Start();
            _pwmRight.Start();
        }

        // Imposta la velocità dei motori (0.0 a 1.0)
        private void SetSpeed(double left, double right)
        {    // Limita tra 0.0 e 1.0
            _pwmLeft.DutyCycle = Math.Clamp(left, 0.0, 1.0);
            _pwmRight.DutyCycle = Math.Clamp(right, 0.0, 1.0);
        }
        
        // Fornisce una "spinta" iniziale per superare l'attrito statico
        private void KickStart(double leftSpeed, double rightSpeed)
        {
            // Duty più alto per superare attrito iniziale
            SetSpeed(Math.Clamp(leftSpeed + KickStartBoost_left, 0.0, 1.0),
                     Math.Clamp(rightSpeed + KickStartBoost_rigth, 0.0, 1.0));

            Task.Delay(KickStartDurationMs).ContinueWith(_ =>
            {
                // Dopo breve intervallo, torna al duty normale
                SetSpeed(leftSpeed, rightSpeed);
            });
        }

        // =================== COMANDI DI MOVIMENTO ===================
        public void MoveForward(double speed = DefaultSpeed)
        {
            _gpio.Write(_leftA, PinValue.High);
            _gpio.Write(_leftB, PinValue.Low);
            _gpio.Write(_rightA, PinValue.High);
            _gpio.Write(_rightB, PinValue.Low);

            KickStart(speed, speed);
        }

        public void MoveBackward(double speed = DefaultSpeed)
        {
            _gpio.Write(_leftA, PinValue.Low);
            _gpio.Write(_leftB, PinValue.High);
            _gpio.Write(_rightA, PinValue.Low);
            _gpio.Write(_rightB, PinValue.High);

            KickStart(speed, speed);
        }

        public void TurnLeft(double speed = DefaultSpeed)
        {
            _gpio.Write(_leftA, PinValue.Low);
            _gpio.Write(_leftB, PinValue.High);
            _gpio.Write(_rightA, PinValue.High);
            _gpio.Write(_rightB, PinValue.Low);

            KickStart(speed, speed);
        }

        public void TurnRight(double speed = DefaultSpeed)
        {
            _gpio.Write(_leftA, PinValue.High);
            _gpio.Write(_leftB, PinValue.Low);
            _gpio.Write(_rightA, PinValue.Low);
            _gpio.Write(_rightB, PinValue.High);

            KickStart(speed, speed);
        }

        public void Stop()
        {
            // Imposta i pin di direzione a LOW e duty a 0
            _gpio.Write(_leftA, PinValue.Low);
            _gpio.Write(_leftB, PinValue.Low);
            _gpio.Write(_rightA, PinValue.Low);
            _gpio.Write(_rightB, PinValue.Low);

            SetSpeed(0, 0);
        }

        public void Dispose()
        {
            Stop();
            _pwmLeft?.Dispose();
            _pwmRight?.Dispose();
            _gpio?.Dispose();
        }
    }
}
