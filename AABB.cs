using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RayTracingInOneWeekend;

internal readonly struct AABB
{
    public Vector3 Min { get; }
    public Vector3 Max { get; }

    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Hit(Ray r, float tMin, float tMax)
    {
        for (int a = 0; a < 3; a++)
        {
            var invD = 1.0f / r.Direction[a];
            var t0 = (Min[a] - r.Origin[a]) * invD;
            var t1 = (Max[a] - r.Origin[a]) * invD;
            if (invD < 0.0f)
            {
                (t0, t1) = (t1, t0);
            }
            tMin = t0 > tMin ? t0 : tMin;
            tMax = t1 < tMax ? t1 : tMax;
            if (tMax <= tMin)
                return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB SurroundingBox(AABB box0, AABB box1)
    {
        var small = new Vector3(
            MathF.Min(box0.Min.X, box1.Min.X),
            MathF.Min(box0.Min.Y, box1.Min.Y),
            MathF.Min(box0.Min.Z, box1.Min.Z));
        var big = new Vector3(
            MathF.Max(box0.Max.X, box1.Max.X),
            MathF.Max(box0.Max.Y, box1.Max.Y),
            MathF.Max(box0.Max.Z, box1.Max.Z));
        return new AABB(small, big);
    }
}
