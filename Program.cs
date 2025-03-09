using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

// Tip: on Windows, make sure to use cmd.exe to run the application. With
// PowerShell, the redirected output ends up with a Unicode byte order mark
// (0xFF, 0xFE) at the beginning of the file. Tools such as display on Linux
// cannot parse a ppm file with byte order mark.

namespace RayTracingInOneWeekend;

internal static class Program
{
    private static readonly Vector3 White = new(1, 1, 1);
    private static readonly Vector3 Black = new(0, 0, 0);
    private static readonly Vector3 Blue = new(0.5f, 0.7f, 1);

    private static Vector3 Color(Ray ray, HitableItems world, int depth, Random rng)
    {
        var color = Vector3.Zero;
        var attenuation = Vector3.One;
        var currentRay = ray;

        for (int i = 0; i < 50; i++)
        {
            var record = new HitRecord();
            if (world.Hit(currentRay, 0.001f, float.MaxValue, ref record))
            {
                if (record.Material.Scatter(currentRay, record, out var tempAttenuation, out var scatterRay))
                {
                    attenuation *= tempAttenuation;
                    currentRay = scatterRay;
                }
                else
                {
                    return color;
                }
            }
            else
            {
                var unitDirection = Vector3.Normalize(currentRay.Direction);
                var t = 0.5f * (unitDirection.Y + 1);
                color += attenuation * ((1 - t) * White + t * Blue);
                return color;
            }
        }

        return color;
    }

    private static HitableItems RandomScene()
    {
        var hitables = new List<Hitable>
        {
            new Sphere(new Vector3(0, -1000f, 0), 1000, new Lambertian(new Vector3(0.5f, 0.5f, 0.5f)))
        };

        var rng = new Random();
        for (var a = -11; a < 11; a++)
        {
            for (var b = -11; b < 11; b++)
            {
                var chooseMaterial = rng.NextSingle();
                var center = new Vector3(a + 0.9f * rng.NextSingle(), 0.2f, b + 0.9f * rng.NextSingle());

                if ((center - new Vector3(4, 0.2f, 0)).Length() > 0.9)
                {
                    if (chooseMaterial < 0.8)
                    {
                        hitables.Add(
                            new Sphere(center, 0.2f,
                                new Lambertian(
                                    new Vector3(
                                        rng.NextSingle() * rng.NextSingle(),
                                        rng.NextSingle() * rng.NextSingle(),
                                        rng.NextSingle() * rng.NextSingle()))));
                    }
                    else if (chooseMaterial < 0.95)
                    {
                        hitables.Add(
                            new Sphere(center, 0.2f,
                                new Metal(
                                    new Vector3(
                                        0.5f * (1 + rng.NextSingle()),
                                        0.5f * (1 + rng.NextSingle()),
                                        0.5f * (1 + rng.NextSingle())), 0.1f)));
                    }
                    else
                        hitables.Add(new Sphere(center, 0.2f, new Dielectric(1.5f)));
                }
            }
        }

        hitables.Add(new Sphere(new Vector3(0, 1, 0), 1, new Dielectric(1.5f)));
        hitables.Add(new Sphere(new Vector3(-4, 1, 0), 1, new Lambertian(new Vector3(0.4f, 0.2f, 0.1f))));
        hitables.Add(new Sphere(new Vector3(4, 1, 0), 1, new Metal(new Vector3(0.7f, 0.6f, 0.5f), 0.0f)));
        return new HitableItems(hitables.ToArray());
    }

    private static void Main()
    {
        const int numX = 1280;
        const int numY = 800;
        const int numSamples = 10;

        var world = RandomScene();
        var lookFrom = new Vector3(13, 2, 3);
        var lookAt = new Vector3(0, 0, 0);
        var distanceToFocus = 10;
        var aperture = 0.1f;
        var camera = new Camera(lookFrom, lookAt, new Vector3(0, 1, 0), 20, numX / (float)numY, aperture, distanceToFocus);

        var pixels = new int[numY, numX, 3];
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var partitioner = Partitioner.Create(0, numY);
        var totalRows = numY;
        var completedRows = 0;

        // Progress indicator task
        var progressTask = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                var progress = (double)completedRows / totalRows;
                var progressBar = new string('#', (int)(progress * 50)).PadRight(50);
                Console.Write($"\r[{progressBar}] {progress:P0}");
                Thread.Sleep(1000); // Update every second
            }
        });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            Parallel.ForEach(partitioner, new ParallelOptions { CancellationToken = token }, (range, state) =>
            {
                var rng = new Random();
                for (var j = range.Item1; j < range.Item2; j++)
                {
                    for (var i = 0; i < numX; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        var col = Vector3.Zero;
                        for (var s = 0; s < numSamples; s++)
                        {
                            var u = (i + rng.NextSingle()) / numX;
                            var v = (j + rng.NextSingle()) / numY;
                            var r = camera.GetRay(u, v);
                            col += Color(r, world, 0, rng);
                        }

                        col /= numSamples;
                        var ir = (int)(255.99 * MathF.Sqrt(col.X));
                        var ig = (int)(255.99 * MathF.Sqrt(col.Y));
                        var ib = (int)(255.99 * MathF.Sqrt(col.Z));
                        pixels[j, i, 0] = ir;
                        pixels[j, i, 1] = ig;
                        pixels[j, i, 2] = ib;
                    }
                    Interlocked.Increment(ref completedRows);
                }
            });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation was canceled.");
        }

        // Wait for the progress task to complete
        cts.Cancel();
        progressTask.Wait();

        stopwatch.Stop();

        // Ensure the final progress bar is complete
        Console.Write($"\r[##################################################] 100%");
        Console.WriteLine($"\nExecution Time: {stopwatch.Elapsed}");

        using (var writer = new StreamWriter("output.ppm"))
        {
            writer.WriteLine($"P3\n{numX} {numY}\n255");
            for (var j = numY - 1; j >= 0; j--)
            {
                for (var i = 0; i < numX; i++)
                {
                    writer.WriteLine($"{pixels[j, i, 0]} {pixels[j, i, 1]} {pixels[j, i, 2]}");
                }
            }
        }
    }
}
