using RayTracingInOneWeekend;
using System.Numerics;

internal interface IMaterial
{
    bool Scatter(Ray incidentRay, HitRecord rec, out Vector3 attenuation, out Ray scatteredRay);
}
