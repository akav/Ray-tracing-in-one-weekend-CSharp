using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RayTracingInOneWeekend;

internal struct BVHNode : Hitable
{
    private readonly Hitable _left;
    private readonly Hitable _right;
    private readonly AABB _box;

    public BVHNode(Hitable[] hitables, int start, int end, Random rng)
    {
        var axis = rng.Next(0, 3);
        Comparison<Hitable> comparator = axis switch
        {
            0 => (a, b) => a.BoundingBox.Min.X.CompareTo(b.BoundingBox.Min.X),
            1 => (a, b) => a.BoundingBox.Min.Y.CompareTo(b.BoundingBox.Min.Y),
            _ => (a, b) => a.BoundingBox.Min.Z.CompareTo(b.BoundingBox.Min.Z),
        };

        var span = end - start;
        if (span == 1)
        {
            _left = _right = hitables[start];
        }
        else if (span == 2)
        {
            if (comparator(hitables[start], hitables[start + 1]) < 0)
            {
                _left = hitables[start];
                _right = hitables[start + 1];
            }
            else
            {
                _left = hitables[start + 1];
                _right = hitables[start];
            }
        }
        else
        {
            Array.Sort(hitables, start, span, Comparer<Hitable>.Create(comparator));
            var mid = start + span / 2;
            _left = new BVHNode(hitables, start, mid, rng);
            _right = new BVHNode(hitables, mid, end, rng);
        }

        _box = AABB.SurroundingBox(_left.BoundingBox, _right.BoundingBox);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Hit(Ray r, float tMin, float tMax, ref HitRecord record)
    {
        if (!_box.Hit(r, tMin, tMax))
            return false;

        var hitLeft = _left.Hit(r, tMin, tMax, ref record);
        var hitRight = _right.Hit(r, tMin, hitLeft ? record.T : tMax, ref record);
        return hitLeft || hitRight;
    }

    public AABB BoundingBox => _box;
}
