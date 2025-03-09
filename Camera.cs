using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RayTracingInOneWeekend;

internal struct Camera
{
    private readonly Vector3 _lowerLeftCorner;
    private readonly Vector3 _horizontal;
    private readonly Vector3 _vertical;
    private readonly Vector3 _origin;
    private readonly Vector3 _u;
    private readonly Vector3 _v;
    private readonly float _lensRadius;
    private readonly Random _rng = Random.Shared;
    private static readonly Vector3 Size = new(1, 1, 0);

    // verticalFieldOfViewDegrees is top to bottom in degrees.
    public Camera(Vector3 lookFrom, Vector3 lookAt, Vector3 viewUp, float verticalFieldOfViewDegrees, float aspectRatio, float aperture, float focusDistance)
    {
        _lensRadius = aperture / 2;
        var theta = verticalFieldOfViewDegrees * MathF.PI / 180;
        var halfHeight = MathF.Tan(theta / 2);
        var halfWidth = aspectRatio * halfHeight;

        _origin = lookFrom;
        var w = Vector3.Normalize(lookFrom - lookAt);
        _u = Vector3.Normalize(Vector3.Cross(viewUp, w));
        _v = Vector3.Cross(w, _u);

        _lowerLeftCorner = _origin - (halfWidth * focusDistance) * _u - (halfHeight * focusDistance) * _v - focusDistance * w;
        _horizontal = 2 * halfWidth * focusDistance * _u;
        _vertical = 2 * halfHeight * focusDistance * _v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ray GetRay(float s, float t)
    {
        var rayDirection = _lensRadius * RandomInUnitDisk();
        var offset = _u * rayDirection.X + _v * rayDirection.Y;
        return new Ray(_origin + offset, _lowerLeftCorner + s * _horizontal + t * _vertical - _origin - offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3 RandomInUnitDisk()
    {
        Vector3 p;
        do
        {
            p = 2 * new Vector3(_rng.NextSingle(), _rng.NextSingle(), 0) - Size;
        }
        while (Vector3.Dot(p, p) >= 1);
        return p;
    }
}
