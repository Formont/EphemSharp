using EphemSharp.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EphemSharp
{
    public abstract class CelestialBody
    {
        public Angle RightAscension { get; set; }
        public Angle Declination { get; set; }
    }
}
