using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RayTracingInOneWeekend;

internal interface IMaterial
{
    bool Scatter(Ray incidentRay, HitRecord rec, out Vector3 attenuation, out Ray scatteredRay);
}

internal struct Lambertian : IMaterial
{
    private readonly Vector3 _albedo;

    public Lambertian(Vector3 albedo)
    {
        _albedo = albedo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Scatter(Ray incidentRay, HitRecord rec, out Vector3 attenuation, out Ray scatteredRay)
    {
        var targetOnUnitSphere = rec.PointOfIntersection + rec.Normal + MaterialHelpers.RandomInUnitSphere();
        scatteredRay = new Ray(rec.PointOfIntersection, targetOnUnitSphere - rec.PointOfIntersection);
        attenuation = _albedo;
        return true;
    }
}

internal struct Metal : IMaterial
{
    private readonly Vector3 _albedo;
    private readonly float _fuzziness;

    public Metal(Vector3 albedo, float fuzziness)
    {
        _albedo = albedo;
        _fuzziness = fuzziness < 1 ? fuzziness : 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Scatter(Ray incidentRay, HitRecord rec, out Vector3 attenuation, out Ray scatteredRay)
    {
        var reflected = MaterialHelpers.Reflect(Vector3.Normalize(incidentRay.Direction), rec.Normal);
        scatteredRay = new Ray(rec.PointOfIntersection, reflected + _fuzziness * MaterialHelpers.RandomInUnitSphere());
        attenuation = _albedo;
        return Vector3.Dot(scatteredRay.Direction, rec.Normal) > 0;
    }
}

internal struct Dielectric : IMaterial
{
    private readonly float _refractionIndex;

    public Dielectric(float refractionIndex)
    {
        _refractionIndex = refractionIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Scatter(Ray incidentRay, HitRecord rec, out Vector3 attenuation, out Ray scatteredRay)
    {
        attenuation = Vector3.One;
        Vector3 outwardNormal;
        float niOverNt;
        float cosine;
        var reflectedRay = MaterialHelpers.Reflect(incidentRay.Direction, rec.Normal);

        if (Vector3.Dot(incidentRay.Direction, rec.Normal) > 0)
        {
            outwardNormal = -rec.Normal;
            niOverNt = _refractionIndex;
            cosine = _refractionIndex * Vector3.Dot(incidentRay.Direction, rec.Normal) / incidentRay.Direction.Length();
        }
        else
        {
            outwardNormal = rec.Normal;
            niOverNt = 1 / _refractionIndex;
            cosine = -Vector3.Dot(incidentRay.Direction, rec.Normal) / incidentRay.Direction.Length();
        }

        var reflectionProbability = MaterialHelpers.Refract(incidentRay.Direction, outwardNormal, niOverNt, out var refractedRay)
            ? MaterialHelpers.Schlick(cosine, _refractionIndex)
            : 1;

        scatteredRay = Random.Shared.NextSingle() < reflectionProbability
            ? new Ray(rec.PointOfIntersection, reflectedRay)
            : new Ray(rec.PointOfIntersection, refractedRay);

        return true;
    }
}

internal static class MaterialHelpers
{
    private static readonly Random Rng = Random.Shared;
    private static readonly Vector3 UnitVector = Vector3.One;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 RandomInUnitSphere()
    {
        Vector3 p;
        do
        {
            p = 2 * new Vector3(Rng.NextSingle(), Rng.NextSingle(), Rng.NextSingle()) - UnitVector;
        }
        while (p.LengthSquared() >= 1);
        return p;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Refract(Vector3 v, Vector3 n, float niOverNt, out Vector3 refractedRay)
    {
        var uv = Vector3.Normalize(v);
        var dt = Vector3.Dot(uv, n);
        var discriminant = 1 - niOverNt * niOverNt * (1 - dt * dt);

        if (discriminant > 0)
        {
            refractedRay = niOverNt * (uv - n * dt) - n * MathF.Sqrt(discriminant);
            return true;
        }

        refractedRay = Vector3.Zero;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Reflect(Vector3 ray, Vector3 normal) => ray - 2 * Vector3.Dot(ray, normal) * normal;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Schlick(float cosine, float refractionIndex)
    {
        var r0 = (1 - refractionIndex) / (1 + refractionIndex);
        r0 *= r0;
        return r0 + (1 - r0) * MathF.Pow(1 - cosine, 5);
    }
}
