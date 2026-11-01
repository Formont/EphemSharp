using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using EphemSharp.Bodies;
using EphemSharp.Data;
using EphemSharp.Enums;
using EphemSharp.Units;

namespace EphemSharp
{
    /// <summary>Reads DAF/SPK kernels and evaluates type 2 and 3 Chebyshev segments.</summary>
    public class SpkKernel
    {
        private const int RecordBytes = 1024;
        private static readonly Dictionary<int, string> SegmentNames = CreateSegmentNames();
        private static readonly Dictionary<string, int> SegmentIdsByName = CreateSegmentIdsByName();
        private readonly List<SpkKernel> linkedKernels;
        private readonly List<SpkSegment> ownSegments;
        private readonly byte[] data;
        private readonly bool littleEndian;

        public string IdWord { get; private set; }
        public string BinaryFormat { get; private set; }
        public string FileName { get; private set; }
        public List<SpkSegment> Segments { get; private set; }

        public SpkKernel(string path) : this(path, new string[0])
        {
        }

        /// <summary>
        /// Opens a primary SPK kernel and links additional kernels that may provide
        /// missing center segments (for example, a satellite kernel plus a DE kernel).
        /// The primary kernel takes precedence when multiple files contain the same target.
        /// </summary>
        public SpkKernel(string path, params string[] additionalKernelPaths)
        {
            if (path == null) throw new ArgumentNullException("path");
            FileName = System.IO.Path.GetFileName(path);
            linkedKernels = new List<SpkKernel>();
            ownSegments = new List<SpkSegment>();
            Segments = new List<SpkSegment>();
            data = File.ReadAllBytes(path);
            if (data.Length < RecordBytes || Encoding.ASCII.GetString(data, 0, 8) != "DAF/SPK ")
                throw new InvalidDataException("File does not have a DAF/SPK header.");

            IdWord = Encoding.ASCII.GetString(data, 0, 8).Trim();
            BinaryFormat = Encoding.ASCII.GetString(data, 88, 8).Trim();
            if (BinaryFormat == "LTL-IEEE") littleEndian = true;
            else if (BinaryFormat == "BIG-IEEE") littleEndian = false;
            else throw new InvalidDataException("Unsupported DAF format: " + BinaryFormat + ".");

            int record = I32(76);
            HashSet<int> visited = new HashSet<int>();
            while (record != 0)
            {
                if (!visited.Add(record)) throw new InvalidDataException("Cycle in summary records chain.");
                long offset = (long)(record - 1) * RecordBytes;
                Need(offset, RecordBytes);
                int next = (int)F64(offset);
                int count = (int)F64(offset + 16);
                if (count < 0 || count > 25) throw new InvalidDataException("Inconsistent number of summary records in DAF.");
                for (int i = 0; i < count; i++)
                {
                    long item = offset + 24 + i * 40;
                    ownSegments.Add(new SpkSegment(this, F64(item), F64(item + 8), I32(item + 16), I32(item + 20),
                        I32(item + 24), I32(item + 28), I32(item + 32), I32(item + 36)));
                }
                record = next;
            }

            Segments.AddRange(ownSegments);
            if (additionalKernelPaths != null)
            {
                for (int i = 0; i < additionalKernelPaths.Length; i++) AddKernel(additionalKernelPaths[i]);
            }
        }

        /// <summary>Links another SPK kernel to supply segments missing from this kernel.</summary>
        public void AddKernel(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            AddKernel(new SpkKernel(path));
        }

        /// <summary>Links an already-open SPK kernel to supply missing center or target segments.</summary>
        public void AddKernel(SpkKernel kernel)
        {
            if (kernel == null) throw new ArgumentNullException("kernel");
            if (ReferenceEquals(kernel, this)) throw new ArgumentException("A kernel cannot be linked to itself.", "kernel");
            if (linkedKernels.Contains(kernel)) return;
            linkedKernels.Add(kernel);
            Segments.AddRange(kernel.Segments);
        }

        /// <summary>Formats the segment ranges and center-to-target chains for every linked kernel.</summary>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            bool firstKernel = true;
            foreach (SpkKernel kernel in EnumerateKernels(new HashSet<SpkKernel>()))
            {
                if (!firstKernel) output.AppendLine();
                firstKernel = false;
                output.Append("Segments from file \"").Append(kernel.FileName).AppendLine("\"");

                double previousStart = double.NaN;
                double previousEnd = double.NaN;
                for (int i = 0; i < kernel.ownSegments.Count; i++)
                {
                    SpkSegment segment = kernel.ownSegments[i];
                    if (i == 0 || segment.StartEt != previousStart || segment.EndEt != previousEnd)
                    {
                        double startJd = 2451545.0 + segment.StartEt / 86400.0;
                        double endJd = 2451545.0 + segment.EndEt / 86400.0;
                        output.Append("JD ")
                            .Append(startJd.ToString("F2", CultureInfo.InvariantCulture))
                            .Append(" - ")
                            .Append(endJd.ToString("F2", CultureInfo.InvariantCulture))
                            .Append(" (")
                            .Append(FormatEtDate(segment.StartEt))
                            .Append(" - ")
                            .Append(FormatEtDate(segment.EndEt))
                            .AppendLine(")");
                        previousStart = segment.StartEt;
                        previousEnd = segment.EndEt;
                    }

                    output.Append(segment.Center.ToString(CultureInfo.InvariantCulture))
                        .Append("->")
                        .Append(segment.Target.ToString(CultureInfo.InvariantCulture))
                        .Append(' ')
                        .Append(GetSegmentName(segment.Center).ToUpperInvariant())
                        .Append(" -> ")
                        .AppendLine(GetSegmentName(segment.Target).ToUpperInvariant());
                }
            }
            return output.ToString().TrimEnd();
        }

        private static string FormatEtDate(double et)
        {
            try
            {
                DateTime j2000Noon = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                return j2000Noon.AddDays(et / 86400.0).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            }
            catch (ArgumentOutOfRangeException)
            {
                return "date out of range";
            }
        }

        /// <summary>Returns the unique targets present in the kernel as display strings such as "999 Pluto".</summary>
        public string[] GetSegmentIds()
        {
            return GetSegmentDescriptions();
        }

        /// <summary>Returns the unique numeric target IDs present in the kernel, in ascending order.</summary>
        public int[] GetSegmentTargetIds()
        {
            HashSet<int> ids = new HashSet<int>();
            for (int i = 0; i < Segments.Count; i++) ids.Add(Segments[i].Target);
            int[] result = new int[ids.Count];
            ids.CopyTo(result);
            Array.Sort(result);
            return result;
        }

        /// <summary>Returns the NAIF name for an ID, or the numeric ID when it is not in the built-in list.</summary>
        public string GetSegmentName(int id)
        {
            string name;
            return SegmentNames.TryGetValue(id, out name) ? name : id.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Resolves a case-insensitive NAIF name, numeric ID, or display value such as "999 Pluto".</summary>
        public int GetSegmentId(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Segment name cannot be empty.", "name");
            string value = name.Trim();
            int numericId;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericId)) return numericId;

            int separator = value.IndexOfAny(new[] { ' ', '\t' });
            if (separator > 0 && int.TryParse(value.Substring(0, separator), NumberStyles.Integer, CultureInfo.InvariantCulture, out numericId))
                return numericId;

            int id;
            if (SegmentIdsByName.TryGetValue(value, out id)) return id;
            throw new KeyNotFoundException("Unknown segment name: " + value);
        }

        /// <summary>Returns IDs present in this kernel as display strings, for example "999 Pluto".</summary>
        public string[] GetSegmentDescriptions()
        {
            int[] ids = GetSegmentTargetIds();
            string[] descriptions = new string[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                string name;
                descriptions[i] = ids[i].ToString(CultureInfo.InvariantCulture) +
                    (SegmentNames.TryGetValue(ids[i], out name) ? " " + name : string.Empty);
            }
            return descriptions;
        }

        /// <summary>Evaluates a named NAIF target, ignoring case in the name.</summary>
    
        public Planet GetPlanet(string name, AstroTime time)
        {
            return GetPlanet(GetSegmentId(name), time);
        }

        public Planet GetPlanet(string name)
        {
            return GetPlanet(name, new AstroTime(DateTime.UtcNow));
        }

        public Planet GetPlanet(int id)
        {
            return GetPlanet(id, new AstroTime(DateTime.UtcNow));
        }

        private static Dictionary<int, string> CreateSegmentNames()
        {
            Dictionary<int, string> names = new Dictionary<int, string>();
            string entries = @"
0|Solar System Barycenter
1|Mercury Barycenter
2|Venus Barycenter
3|Earth-Moon Barycenter
4|Mars Barycenter
5|Jupiter Barycenter
6|Saturn Barycenter
7|Uranus Barycenter
8|Neptune Barycenter
9|Pluto Barycenter
10|Sun
199|Mercury
299|Venus
399|Earth
499|Mars
599|Jupiter
699|Saturn
799|Uranus
899|Neptune
999|Pluto
301|Moon
401|Phobos
402|Deimos
501|Io
502|Europa
503|Ganymede
504|Callisto
505|Amalthea
506|Himalia
507|Elara
508|Pasiphae
509|Sinope
510|Lysithea
511|Carme
512|Ananke
513|Leda
514|Thebe
515|Adrastea
516|Metis
517|Callirrhoe
518|Themisto
519|Megaclite
520|Taygete
521|Chaldene
522|Harpalyke
523|Kalyke
524|Iocaste
525|Erinome
526|Isonoe
527|Praxidike
528|Autonoe
529|Thyone
530|Hermippe
531|Aitne
532|Eurydome
533|Euanthe
534|Euporie
535|Orthosie
536|Sponde
537|Kale
538|Pasithee
539|Hegemone
540|Mneme
541|Aoede
542|Thelxinoe
543|Arche
544|Kallichore
545|Helike
546|Carpo
547|Eukelade
548|Cyllene
549|Kore
550|Herse
553|Dia
601|Mimas
602|Enceladus
603|Tethys
604|Dione
605|Rhea
606|Titan
607|Hyperion
608|Iapetus
609|Phoebe
610|Janus
611|Epimetheus
612|Helene
613|Telesto
614|Calypso
615|Atlas
616|Prometheus
617|Pandora
618|Pan
619|Ymir
620|Paaliaq
621|Tarvos
622|Ijiraq
623|Suttungr
624|Kiviuq
625|Mundilfari
626|Albiorix
627|Skathi
628|Erriapus
629|Siarnaq
630|Thrymr
631|Narvi
632|Methone
633|Pallene
634|Polydeuces
635|Daphnis
636|Aegir
637|Bebhionn
638|Bergelmir
639|Bestla
640|Farbauti
641|Fenrir
642|Fornjot
643|Hati
644|Hyrrokkin
645|Kari
646|Loge
647|Skoll
648|Surtur
649|Anthe
650|Jarnsaxa
651|Greip
652|Tarqeq
653|Aegaeon
701|Ariel
702|Umbriel
703|Titania
704|Oberon
705|Miranda
706|Cordelia
707|Ophelia
708|Bianca
709|Cressida
710|Desdemona
711|Juliet
712|Portia
713|Rosalind
714|Belinda
715|Puck
716|Caliban
717|Sycorax
718|Prospero
719|Setebos
720|Stephano
721|Trinculo
722|Francisco
723|Margaret
724|Ferdinand
725|Perdita
726|Mab
727|Cupid
801|Triton
802|Nereid
803|Naiad
804|Thalassa
805|Despina
806|Galatea
807|Larissa
808|Proteus
809|Halimede
810|Psamathe
811|Sao
812|Laomedeia
813|Neso
901|Charon
902|Nix
903|Hydra
904|Kerberos
905|Styx";
            string[] lines = entries.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] pair = lines[i].Split(new[] { '|' }, 2);
                names.Add(int.Parse(pair[0], CultureInfo.InvariantCulture), pair[1]);
            }
            return names;
        }

        private static Dictionary<string, int> CreateSegmentIdsByName()
        {
            Dictionary<string, int> ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<int, string> item in SegmentNames) ids.Add(item.Value, item.Key);
            return ids;
        }

        /// <summary>Returns the first state segment for target id at the supplied time.</summary>
        public Planet GetPlanet(int id, AstroTime time)
        {
            if (time == null) throw new ArgumentNullException("time");
            double et = time.EphemerisTime;
            HashSet<int> visited = new HashSet<int>();
            Vector target = PositionRelativeToSsb(id, et, visited);
            Vector earth;
            try
            {
                earth = PositionRelativeToSsb(399, et, new HashSet<int>());
            }
            catch (ArgumentOutOfRangeException)
            {
                // DE kernels commonly provide the Earth-Moon barycenter (3), not Earth center (399).
                earth = PositionRelativeToSsb(3, et, new HashSet<int>());
                try
                {
                    Vector moonFromEmb = PositionRelativeToSsb(301, et, new HashSet<int>()) - earth;
                    const double moonToEarthMassRatio = 0.0123000371;
                    earth = earth - moonFromEmb * (moonToEarthMassRatio / (1.0 + moonToEarthMassRatio));
                }
                catch (ArgumentOutOfRangeException) { }
            }
            Vector sun = PositionRelativeToSsb(10, et, new HashSet<int>());
            Vector geocentricKm = target - earth;
            Vector heliocentricKm = target - sun;
            double distanceAu = geocentricKm.Length() / Distance.AU_KM;
            Vector geocentricAu = geocentricKm * (1.0 / Distance.AU_KM);
            Vector heliocentricAu = heliocentricKm * (1.0 / Distance.AU_KM);
            // SPK frame 1 (J2000) is equatorial. VSOP87 vectors in this library are
            // ecliptic and are converted by MathUtils.ToRaDec, so calculate RA/Dec
            // directly from the SPK equatorial vector here.
            double ra = Math.Atan2(geocentricAu.Y, geocentricAu.X);
            if (ra < 0) ra += 2 * Math.PI;
            double dec = distanceAu == 0 ? 0 : Math.Asin(geocentricAu.Z / geocentricAu.Length());
            double sunDistanceAu = heliocentricAu.Length();
            Planets planetType;
            double radiusKm;
            bool hasPhysicalData = TryGetPhysicalData(id, out planetType, out radiusKm);
            double earthSunDistanceAu = (earth - sun).Length() / Distance.AU_KM;
            Angle phaseAngle;
            if (id == 10 || distanceAu == 0 || sunDistanceAu == 0)
                phaseAngle = new Angle(AngleType.Degrees, 0);
            else
                phaseAngle = MathUtils.CountPhaseAngle(sunDistanceAu, distanceAu, earthSunDistanceAu);

            double angularDiameter = 0;
            double magnitude = 99.99;
            if (hasPhysicalData && distanceAu > 0)
            {
                double radiusRatio = radiusKm / (distanceAu * Distance.AU_KM);
                if (radiusRatio > 1) radiusRatio = 1;
                angularDiameter = 2 * Math.Asin(radiusRatio);
                magnitude = MagnitudeCounter.GetMagnitude(planetType, sunDistanceAu, distanceAu,
                    phaseAngle.GetDegrees(), ra, dec);
            }
            Planet result = new Planet(new Distance(au: distanceAu), new Distance(au: sunDistanceAu),
                new Angle(AngleType.Hours, ra), new Angle(AngleType.Degrees, dec),
                new Angle(AngleType.Degrees, angularDiameter), phaseAngle, magnitude,
                heliocentricAu);
            result.EphemerisProvider = delegate(AstroTime requestedTime) { return GetPlanet(id, requestedTime); };
            return result;
        }

        private static bool TryGetPhysicalData(int id, out Planets planet, out double radiusKm)
        {
            switch (id)
            {
                case 1: case 199: planet = Planets.Mercury; radiusKm = 2440.53; return true;
                case 2: case 299: planet = Planets.Venus; radiusKm = 6051.8; return true;
                case 3: case 399: planet = Planets.Earth; radiusKm = 6378.1366; return true;
                case 4: case 499: planet = Planets.Mars; radiusKm = 3396.19; return true;
                case 5: case 599: planet = Planets.Jupiter; radiusKm = 71492.0; return true;
                case 6: case 699: planet = Planets.Saturn; radiusKm = 60268.0; return true;
                case 7: case 799: planet = Planets.Uranus; radiusKm = 25559.0; return true;
                case 8: case 899: planet = Planets.Neptune; radiusKm = 24764.0; return true;
                case 9: case 999: planet = Planets.Pluto; radiusKm = 1188.3; return true;
                case 10: planet = Planets.Sun; radiusKm = 695700.0; return true;
                case 301: planet = Planets.Moon; radiusKm = 1737.4; return true;
                default: planet = Planets.Mercury; radiusKm = 0; return false;
            }
        }

        private Vector PositionRelativeToSsb(int target, double et, HashSet<int> visited)
        {
            return PositionRelativeToSsb(target, et, visited, null);
        }

        private Vector PositionRelativeToSsb(int target, double et, HashSet<int> visited, int? expectedFrame)
        {
            if (target == 0) return new Vector();
            if (!visited.Add(target)) throw new InvalidDataException("Cycle detected in the SPK center chain.");
            SpkSegment segment = FindSegment(target, et);
            if (segment == null)
                throw new ArgumentOutOfRangeException("target", "No SPK segment was found for target ID " + target + " at the requested time. " +
                    "If this ID is a satellite's center, link a DE kernel containing the corresponding barycenter.");
            if (expectedFrame.HasValue && segment.Frame != expectedFrame.Value)
                throw new NotSupportedException("Нельзя объединить SPK-сегменты в разных системах координат (frame " +
                    expectedFrame.Value + " и " + segment.Frame + "). Преобразование frame пока не поддерживается.");
            Vector relativePosition = segment.Owner.Evaluate(segment, et).Position;
            return relativePosition + PositionRelativeToSsb(segment.Center, et, visited, segment.Frame);
        }

        private SpkSegment FindSegment(int target, double et)
        {
            // The primary kernel has precedence; within each file, later summaries
            // have precedence over earlier ones, matching SPK segment precedence.
            foreach (SpkKernel kernel in EnumerateKernels(new HashSet<SpkKernel>()))
            {
                for (int i = kernel.ownSegments.Count - 1; i >= 0; i--)
                {
                    SpkSegment candidate = kernel.ownSegments[i];
                    if (candidate.Target == target && et >= candidate.StartEt && et <= candidate.EndEt)
                        return candidate;
                }
            }
            return null;
        }

        private IEnumerable<SpkKernel> EnumerateKernels(HashSet<SpkKernel> visited)
        {
            if (!visited.Add(this)) yield break;
            yield return this;
            for (int i = 0; i < linkedKernels.Count; i++)
            {
                foreach (SpkKernel linked in linkedKernels[i].EnumerateKernels(visited)) yield return linked;
            }
        }

        private State Evaluate(SpkSegment segment, double et)
        {
            if (segment.Type != 2 && segment.Type != 3)
                throw new NotSupportedException("Type SPK " + segment.Type + " is not supported (supported types are 2 and 3).");
            long baseOffset = AddressOffset(segment.StartAddress);
            long endOffset = AddressOffset(segment.EndAddress);
            Need(baseOffset, endOffset - baseOffset + 8);
            double initial = F64(endOffset - 24);
            double intervalLength = F64(endOffset - 16);
            int recordSize = (int)F64(endOffset - 8);
            int count = (int)F64(endOffset);
            if (intervalLength <= 0 || recordSize < 5 || count < 1 || baseOffset + (long)recordSize * count * 8 != endOffset - 24)
                throw new InvalidDataException("Inconsistent Chebyshev index in segment.");
            int index = (int)Math.Floor((et - initial) / intervalLength);
            if (index < 0) index = 0;
            if (index >= count) index = count - 1;
            long offset = baseOffset + (long)index * recordSize * 8;
            double midpoint = F64(offset);
            double radius = F64(offset + 8);
            if (radius <= 0) throw new InvalidDataException("Inconsistent interval radius.");
            // SPK record radius is already half the interval width, so this
            // normalization must map the record endpoints to -1 and +1.
            double x = (et - midpoint) / radius;
            int groups = segment.Type == 2 ? 3 : 6;
            if ((recordSize - 2) % groups != 0) throw new InvalidDataException("Inconsistent record size");
            int coefficients = (recordSize - 2) / groups;
            double[] p = new double[3];
            double[] v = new double[3];
            for (int axis = 0; axis < 3; axis++)
            {
                double[] pc = new double[coefficients];
                for (int j = 0; j < coefficients; j++) pc[j] = F64(offset + (2 + axis * coefficients + j) * 8);
                Tuple<double, double> result = Chebyshev(pc, x);
                p[axis] = result.Item1;
                if (segment.Type == 2) v[axis] = result.Item2 / radius;
                else
                {
                    double[] vc = new double[coefficients];
                    for (int j = 0; j < coefficients; j++) vc[j] = F64(offset + (2 + (axis + 3) * coefficients + j) * 8);
                    v[axis] = Chebyshev(vc, x).Item1;
                }
            }
            return new State(new Vector(p[0], p[1], p[2]), new Vector(v[0], v[1], v[2]));
        }

        private static Tuple<double, double> Chebyshev(double[] c, double x)
        {
            double b1 = 0, b2 = 0, d1 = 0, d2 = 0;
            for (int k = c.Length - 1; k >= 1; k--)
            {
                double b0 = 2 * x * b1 - b2 + c[k];
                double d0 = 2 * b1 + 2 * x * d1 - d2;
                b2 = b1; b1 = b0; d2 = d1; d1 = d0;
            }
            return Tuple.Create(x * b1 - b2 + c[0], b1 + x * d1 - d2);
        }

        private long AddressOffset(int address) { return checked(((long)address - 1) * 8); }
        private void Need(long offset, long length)
        {
            if (offset < 0 || length < 0 || offset > data.Length || length > data.Length - offset)
                throw new InvalidDataException("Address of Data out of bounds");
        }
        private int I32(long offset)
        {
            Need(offset, 4);
            byte[] b = new byte[4]; Array.Copy(data, (int)offset, b, 0, 4);
            if (!littleEndian) Array.Reverse(b);
            return BitConverter.ToInt32(b, 0);
        }
        private double F64(long offset)
        {
            Need(offset, 8);
            byte[] b = new byte[8]; Array.Copy(data, (int)offset, b, 0, 8);
            if (BitConverter.IsLittleEndian != littleEndian) Array.Reverse(b);
            return BitConverter.ToDouble(b, 0);
        }

        private sealed class State
        {
            public Vector Position;
            public Vector Velocity;
            public State(Vector position, Vector velocity) { Position = position; Velocity = velocity; }
        }
    }

    public sealed class SpkSegment
    {
        internal SpkKernel Owner { get; private set; }
        public double StartEt { get; private set; }
        public double EndEt { get; private set; }
        public int Target { get; private set; }
        public int Center { get; private set; }
        public int Frame { get; private set; }
        public int Type { get; private set; }
        public int StartAddress { get; private set; }
        public int EndAddress { get; private set; }

        internal SpkSegment(SpkKernel owner, double startEt, double endEt, int target, int center, int frame, int type, int startAddress, int endAddress)
        {
            Owner = owner;
            StartEt = startEt; EndEt = endEt; Target = target; Center = center; Frame = frame; Type = type;
            StartAddress = startAddress; EndAddress = endAddress;
        }
    }
}
