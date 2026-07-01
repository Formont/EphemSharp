using EphemSharp.Units;
using EphemSharp.Enums;

namespace EphemSharp
{
    public class ObservedObject
    {
        public Angle Altitude { get; private set; }
        public Angle Azimuth { get; private set; }
        public Angle HourAngle { get; private set; }
        public Distance Distance { get; private set; }
        public Angle RightAscension { get; private set; }
        public Angle Declination { get; private set; }
        public Angle AngularSize { get; private set; }

        public ObservedObject(Angle altitude, Angle azimuth, Angle hourAnge, Distance distance = null, Angle? angularSize = null)
        {
            HourAngle = hourAnge;
            Altitude = altitude;
            Azimuth = azimuth;
            Distance = distance;
            RightAscension = new Angle(AngleType.Hours, 0.0);
            Declination = new Angle(AngleType.Degrees, 0.0);
            AngularSize = angularSize ?? new Angle(AngleType.Degrees, 0.0);
        }

        public ObservedObject(Angle altitude, Angle azimuth, Angle hourAnge, Angle rightAscension, Angle declination, Distance distance = null, Angle? angularSize = null)
        {
            HourAngle = hourAnge;
            Altitude = altitude;
            Azimuth = azimuth;
            Distance = distance;
            RightAscension = rightAscension;
            Declination = declination;
            AngularSize = angularSize ?? new Angle(AngleType.Degrees, 0.0);
        }
    }
}
