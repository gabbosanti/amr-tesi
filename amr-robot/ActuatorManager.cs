using System;
using System.Device.Pwm;
using Iot.Device.ServoMotor;
using Iot.Device.Pwm;
using System.Device.Pwm.Drivers;

namespace RobotApp
{   

public class ActuatorManager
{
    // =================== MOTORI (PWM hardware) ===================
    private readonly int IN1_rightMotor = 17;
    private readonly int IN2_rightMotor = 27;
    private readonly int IN3_leftMotor = 22;
    private readonly int IN4_leftMotor = 10;
    private readonly int servoPin = 8; // GPIO per il servo
    private readonly int maximumAngle = 180; // Angolo massimo del servo
    private readonly int minimumPulseWidthMicroseconds = 1000; // Impulso minimo
    private readonly int maximumPulseWidthMicroseconds = 2000; // Impulso massimo
    private readonly int frequency = 50; // Frequenza PWM per il servo
    private readonly double dutyCycle = 0.075; // Duty cycle iniziale per il servo (7.5% per 90°)

    private readonly MotorController _motorController;

    // =================== SERVO (PWM software) ===================
    private readonly ServoMotor _servo;
    SoftwarePwmChannel pwmServo;
    private double? _lastServoAngle = null;

    public ActuatorManager()
    {
        // Motori DC
        _motorController = new MotorController( IN3_leftMotor, IN4_leftMotor, IN1_rightMotor, IN2_rightMotor);

        // Servo su Software PWM (50Hz standard)
        pwmServo = new SoftwarePwmChannel(servoPin, frequency, dutyCycle);

        _servo = new ServoMotor(pwmServo, maximumAngle, minimumPulseWidthMicroseconds, maximumPulseWidthMicroseconds);
        _servo.Start();

        // Imposto angolo iniziale al centro (90°)
        _servo.WriteAngle(90);

    }
    // =================== MOTORI ===================
    public void MoveForward() => _motorController.MoveForward();
    public void MoveBackward() => _motorController.MoveBackward();
    public void StopMotors() => _motorController.Stop();
    public void TurnLeft() => _motorController.TurnLeft();
    public void TurnRight() => _motorController.TurnRight();

    // =================== SERVO ===================
    public void SetServoAngle(double relativeAngleDegrees)
    {
        // Mappa -90..0..90 in 0..180 gradi
        double servoAngle = 90 + relativeAngleDegrees;

        // Limita tra 0 e 180
        servoAngle = Math.Clamp(servoAngle, 0, 180);

        // Evita scritture inutili
        if (_lastServoAngle.HasValue && Math.Abs(_lastServoAngle.Value - servoAngle) < 0.1)
            return;

        _servo.WriteAngle(servoAngle);
        _lastServoAngle = servoAngle;
    }

    // =================== SHUTDOWN ===================
    public void Shutdown()
     {
            _motorController.Stop();
            _servo.Stop();
            pwmServo.Stop();
            pwmServo.Dispose();
     }
}
}