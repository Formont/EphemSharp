using EphemSharp.Units;
namespace EphemSharp
{
    public class ObservedObject
    {
        public Angle Altitude { get; private set; }
        public Angle Azimuth { get; private set; }
        public Angle HourAngle { get; private set; }

        public ObservedObject(Angle altitude, Angle azimuth, Angle hourAnge)
        {
            HourAngle = hourAnge;
            Altitude = altitude;
            Azimuth = azimuth;
        }
    }
}
