using System;
using System.Device.I2c;
using System.Threading;

public class QMC5883L : IDisposable
{   
    private readonly I2cDevice _i2cDevice;

    private const byte QMC5883L_Address = 0x0D;

    // Registri principali
    private const byte REG_X_LSB = 0x00;
    private const byte REG_Y_LSB = 0x02;
    private const byte REG_Z_LSB = 0x04;
    private const byte REG_CTRL1 = 0x09;

    public QMC5883L(I2cDevice i2cDevice)
    {
        _i2cDevice = i2cDevice ?? throw new ArgumentNullException(nameof(i2cDevice));
        Initialize();
    }

    // Configura il sensore
    private void Initialize()
    {
        _i2cDevice.Write(new byte[] { REG_CTRL1, 0b_0001_0001 });
        Thread.Sleep(10);
    }

    // Legge i dati grezzi dal sensore
    public (int X, int Y, int Z) ReadRawData()
    {
        Span<byte> rawData = stackalloc byte[6];

        // Scrive il registro di partenza
        _i2cDevice.WriteByte(REG_X_LSB);

        // Legge 6 byte (X LSB/MSB, Y LSB/MSB, Z LSB/MSB)
        _i2cDevice.Read(rawData);

        int x = (short)(rawData[0] | (rawData[1] << 8));
        int y = (short)(rawData[2] | (rawData[3] << 8));
        int z = (short)(rawData[4] | (rawData[5] << 8));

        return (x, y, z);
    }

    // Ritorna l’heading in gradi (0-360)
    public double GetHeadingDegrees()
    {
        var (x, y, _) = ReadRawData();
        double heading = Math.Atan2(y, x) * (180.0 / Math.PI);
        if (heading < 0)
            heading += 360.0;
        return heading;
    }

    public void Dispose()
    {
        _i2cDevice?.Dispose();
    }
}
