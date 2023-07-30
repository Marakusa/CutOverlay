﻿namespace ColorThief;

/// <summary>
///     Color map
/// </summary>
internal class CMap
{
    private readonly List<VBox> vboxes = new();
    private List<QuantizedColor> palette;

    public void Push(VBox box)
    {
        palette = null;
        vboxes.Add(box);
    }

    public List<QuantizedColor> GeneratePalette()
    {
        if (palette == null)
            palette = (from vBox in vboxes
                let rgb = vBox.Avg(false)
                let color = FromRgb(rgb[0], rgb[1], rgb[2])
                select new QuantizedColor(color, vBox.Count(false))).ToList();

        return palette;
    }

    public int Size()
    {
        return vboxes.Count;
    }

    public int[] Map(int[] color)
    {
        foreach (VBox vbox in vboxes.Where(vbox => vbox.Contains(color))) return vbox.Avg(false);
        return Nearest(color);
    }

    public int[] Nearest(int[] color)
    {
        double d1 = double.MaxValue;
        int[] pColor = null;

        foreach (VBox t in vboxes)
        {
            int[] vbColor = t.Avg(false);
            double d2 = Math.Sqrt(Math.Pow(color[0] - vbColor[0], 2)
                                  + Math.Pow(color[1] - vbColor[1], 2)
                                  + Math.Pow(color[2] - vbColor[2], 2));
            if (d2 < d1)
            {
                d1 = d2;
                pColor = vbColor;
            }
        }

        return pColor;
    }

    public VBox FindColor(double targetLuma, double minLuma, double maxLuma, double targetSaturation,
        double minSaturation, double maxSaturation)
    {
        VBox max = null;
        double maxValue = 0;
        int highestPopulation = vboxes.Select(p => p.Count(false)).Max();

        foreach (VBox swatch in vboxes)
        {
            int[] avg = swatch.Avg(false);
            HslColor hsl = FromRgb(avg[0], avg[1], avg[2]).ToHsl();
            double sat = hsl.S;
            double luma = hsl.L;

            if (sat >= minSaturation && sat <= maxSaturation &&
                luma >= minLuma && luma <= maxLuma)
            {
                double thisValue = Mmcq.CreateComparisonValue(sat, targetSaturation, luma, targetLuma,
                    swatch.Count(false), highestPopulation);

                if (max == null || thisValue > maxValue)
                {
                    max = swatch;
                    maxValue = thisValue;
                }
            }
        }

        return max;
    }

    public Color FromRgb(int red, int green, int blue)
    {
        Color color = new Color
        {
            A = 255,
            R = (byte)red,
            G = (byte)green,
            B = (byte)blue
        };

        return color;
    }
}