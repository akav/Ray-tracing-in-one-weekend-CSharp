using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RayTracingInOneWeekend;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct Ray(Vector3 origin, Vector3 direction)
{
    public Vector3 Origin { get; } = origin;
    public Vector3 Direction { get; } = direction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 PointAtParameter(float t) => Origin + Direction * t;
}
