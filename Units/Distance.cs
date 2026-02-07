namespace EphemSharp.Units
{
    public class Distance
    {
        public double AU { get; set; }
        public double KM { get; set; }

        internal const double AU_KM = 149597870.700;
        internal const double C_KM = 299792.458;
        internal const double LY_KM = 9460730472580.8;
        public Distance(double au = 0, double km = 0) 
        { 
            if (au > 0)
            {
                AU = au;
                KM = au * AU_KM;
            }
            if (km > 0)
            {
                KM = km;
                AU = km / AU_KM;
            }
        }
        public double ToLightSeconds()
        {
            return KM / C_KM;
        }

        public double ToLightYears()
        {
            return KM / LY_KM;
        }
        public static implicit operator string(Distance d)=>d.ToString();

        public override string ToString()
        {
            return string.Format("{0} au", AU);
        }
    }
}
