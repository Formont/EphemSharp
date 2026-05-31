# EphemSharp
**EphemSharp** is a **hobbyist, non-professional astronomy library for C#**.  
It allows you to compute basic ephemerides of stars and planets, as well as their apparent position in the sky for a given observer on Earth.

This project is created for **learning, experimentation, and personal projects**.

---

## ✨ Features

### 🌟 Stars
- Right Ascension (RA) and Declination (Dec)
- Conversion to Altitude / Azimuth
- Constellation determination (IAU boundaries)
- Supports stars from the **Hipparcos catalog (HIP)**.

### 🪐 Planets
- Heliocentric rectangular coordinates (VSOP87)
- Geocentric coordinates
- Right Ascension and Declination
- Angular size
- Phase angle and illumination
- Apparent visual magnitude

### 🌍 Observer
- Altitude and azimuth for any Earth location
- Hour angle
- Local sidereal time

### ⏱ Time
- Julian Date support
- UTC-based calculations

---
## 🚀 Quick Example
---
### Observing a star 
```C#
var obs = new Observer(55.75583, 37.6173); //Moscow
Star star = new Star(new Angle(AngleType.Hours, 22, 57, 38.35), new Angle(AngleType.Degrees, -29, 37, 35.3));//Fomalhaut star
var fomalhaut = obs.Observe(star);
Console.WriteLine(fomalhaut.Altitude);
Console.WriteLine(fomalhaut.Azimuth);

//also you can:
var hip = Catalogs.LoadHipparcos();
star = Star.FromHIP(113368, hip);//Fomalhaut star
var conmap = Constellations.LoadConstellationMap();
var con = Constellations.FindConstellation(star, conmap);
Console.WriteLine(con); //Piscis Austrinus
```
### Getting a planet 
```C#
var mars = Planet.GetPlanet(Planets.Mars);
Console.WriteLine(mars.RightAscension);
Console.WriteLine(mars.Declination);
Console.WriteLine(mars.Magnitude);
Console.WriteLine(mars.AngularSize);
```
### Angular distance 
```C#
for (int i = 0; i <= 365.25 * 100; i++)
{
    var newdate = DateTime.UtcNow.AddDays(i);
    Planet ven = Planet.GetPlanet(Planets.Venus, newdate);
    Planet mer = Planet.GetPlanet(Planets.Mercury, newdate);
    var dst = Calculator.AngularDistance(ven, mer); 
    if ((double)dst <= 0.5)
    {
        Console.WriteLine($"{dst} {newdate}");
    }
}
```
### Transtit, setting, rising
```C#
var obs = new Observer(-33.92487, 18.42406); //Cape Town
var jup = Planet.GetPlanet(Planets.Jupiter);
var transitTime = AstronomicalEvents.FindTransitTime(obs, jup, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
DateTime? setTime = AstronomicalEvents.FindSetTime(obs, jup, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
DateTime? riseTime = AstronomicalEvents.FindRiseTime(obs, jup, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
```
---
### ⚠️ Disclaimer

**EphemSharp is an amateur project.**

- ❌ Not scientifically validated

- ❌ Not suitable for navigation, research, or professional astronomy

- ❌ No guarantee of precision or long-term accuracy

All calculations are provided **“as is”**.
Use this library **at your own risk**.

### 📚 References
- [VSOP87 planetary theory](https://www.neoprogrammics.com/vsop87/source_code_generator_tool/)
- [Skyfield (Python) — used as conceptual inspiration](https://rhodesmill.org/skyfield/)
- IAU constellation boundaries
