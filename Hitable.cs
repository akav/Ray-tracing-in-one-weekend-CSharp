using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RayTracingInOneWeekend;

internal struct HitRecord
{
    public float T;
    public Vector3 PointOfIntersection;
    public Vector3 Normal;
    public IMaterial Material;
}

internal interface Hitable
{
    bool Hit(Ray r, float tMin, float tMax, ref HitRecord record);
    AABB BoundingBox { get; }
}


internal struct HitableItems : Hitable
{
    private readonly Hitable _root;

    public HitableItems(Hitable[] hitables)
    {
        var rng = new Random();
        _root = new BVHNode(hitables, 0, hitables.Length, rng);
    }

    public bool Hit(Ray r, float tMin, float tMax, ref HitRecord record)
    {
        return _root.Hit(r, tMin, tMax, ref record);
    }

    public AABB BoundingBox => _root.BoundingBox;
}

internal struct Sphere : Hitable
{
    private readonly Vector3 _center;
    private readonly float _radius;
    private readonly IMaterial _material;

    public Sphere(Vector3 center, float radius, IMaterial material)
    {
        _center = center;
        _radius = radius;
        _material = material;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Hit(Ray r, float tMin, float tMax, ref HitRecord record)
    {
        var oc = r.Origin - _center;
        var a = Vector3.Dot(r.Direction, r.Direction);
        var b = Vector3.Dot(oc, r.Direction);
        var c = Vector3.Dot(oc, oc) - _radius * _radius;
        var discriminant = b * b - a * c;

        if (discriminant > 0)
        {
            var sqrtDiscriminant = MathF.Sqrt(discriminant);
            var solution1 = (-b - sqrtDiscriminant) / a;
            if (solution1 < tMax && solution1 > tMin)
            {
                record.T = solution1;
                record.PointOfIntersection = r.PointAtParameter(record.T);
                record.Normal = Vector3.Divide(record.PointOfIntersection - _center, _radius);
                record.Material = _material;
                return true;
            }

            var solution2 = (-b + sqrtDiscriminant) / a;
            if (solution2 < tMax && solution2 > tMin)
            {
                record.T = solution2;
                record.PointOfIntersection = r.PointAtParameter(record.T);
                record.Normal = Vector3.Divide(record.PointOfIntersection - _center, _radius);
                record.Material = _material;
                return true;
            }
        }

        return false;
    }

    public AABB BoundingBox => new AABB(_center - new Vector3(_radius), _center + new Vector3(_radius));
}