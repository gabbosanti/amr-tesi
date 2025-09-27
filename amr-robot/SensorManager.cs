using System;
using System.Device.I2c;
using Iot.Device.Hcsr04;

namespace RobotApp
{   

public sealed class SensorManager : IDisposable
{
    private const int UltrasonicTriggerPin = 23;
    private const int UltrasonicEchoPin = 24;

    private const int I2cBusId = 1;
    private const int QMC5883L_Address = 0x0D;

    private readonly Hcsr04 _ultrasonic;
    private readonly QMC5883L _compass;

    //Costruttore
    public SensorManager()
    {
        // Sensore ultrasonico HCSR04
        _ultrasonic = new Hcsr04(UltrasonicTriggerPin, UltrasonicEchoPin);
        if (_ultrasonic == null)
            throw new InvalidOperationException("Sensore ultrasonico non disponibile.");
        
        // Sensore bussola QMC5883L
        try {
            var i2cSettings = new I2cConnectionSettings(I2cBusId, QMC5883L_Address);
            var i2cBus = I2cDevice.Create(i2cSettings);
            _compass = new QMC5883L(i2cBus);
        } catch (Exception ex){
             throw new InvalidOperationException("Impossibile inizializzare il sensore bussola.", ex);
        }
    }

    // Restituisce la distanza in metri (null se errore di lettura runtime)
    public double? GetDistance() => _ultrasonic.TryGetDistance(out var d) ? d.Meters : null;

    // Restituisce l'angolo in gradi [0-360] dal sensore bussola
    public double GetHeadingDegrees() => _compass.GetHeadingDegrees();

    // Pulizia risorse
    public void Dispose()
    {
        _ultrasonic?.Dispose();
        _compass?.Dispose();
    }
}
}   