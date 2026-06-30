using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Windows.Media.Color;

namespace WidgX.Widgets.NowPlaying;

/// <summary>
/// Picks a handful of vibrant "highlight" colors from an image's pixels.
///
/// Algorithm: quantize each opaque pixel into a 5-bit-per-channel bucket, average
/// the pixels in each bucket, then score buckets by how many pixels they hold
/// weighted toward saturated, mid-brightness colors (so washed-out or near-black/
/// white areas don't dominate). The highest-scoring, sufficiently-distinct buckets
/// are returned.
/// </summary>
public static class PaletteExtractor
{
    public static List<Color> Extract(byte[] bgra, int width, int height, int maxColors)
    {
        var buckets = new Dictionary<int, (long R, long G, long B, int Count)>();

        for (var i = 0; i + 3 < bgra.Length; i += 4)
        {
            byte b = bgra[i], g = bgra[i + 1], r = bgra[i + 2], a = bgra[i + 3];
            if (a < 128) continue;

            var key = ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);
            buckets.TryGetValue(key, out var acc);
            buckets[key] = (acc.R + r, acc.G + g, acc.B + b, acc.Count + 1);
        }

        var scored = buckets.Values
            .Select(v =>
            {
                double r = (double)v.R / v.Count, g = (double)v.G / v.Count, b = (double)v.B / v.Count;
                var max = Math.Max(r, Math.Max(g, b));
                var min = Math.Min(r, Math.Min(g, b));
                var saturation = max <= 0 ? 0 : (max - min) / max;
                var brightness = max / 255.0;
                var brightnessWeight = Math.Max(0.2, 1 - Math.Abs(brightness - 0.55) * 1.2);
                var score = v.Count * (0.25 + saturation) * brightnessWeight;
                return (Color: ToColor(r, g, b), Score: score, R: r, G: g, B: b);
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var picked = new List<(Color Color, double R, double G, double B)>();
        foreach (var candidate in scored)
        {
            if (picked.Count >= maxColors) break;
            if (picked.Any(p => Distance(p.R, p.G, p.B, candidate.R, candidate.G, candidate.B) < 48)) continue;
            picked.Add((candidate.Color, candidate.R, candidate.G, candidate.B));
        }

        if (picked.Count == 0 && scored.Count > 0)
        {
            var top = scored[0];
            picked.Add((top.Color, top.R, top.G, top.B));
        }

        return picked.Select(p => p.Color).ToList();
    }

    private static double Distance(double r1, double g1, double b1, double r2, double g2, double b2)
        => Math.Sqrt((r1 - r2) * (r1 - r2) + (g1 - g2) * (g1 - g2) + (b1 - b2) * (b1 - b2));

    private static Color ToColor(double r, double g, double b)
        => Color.FromRgb((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255));
}
