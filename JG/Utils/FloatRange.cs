using System;
using UnityEngine;

[Serializable]
public struct FloatRange
{
    public float min;
    public float max;

    public FloatRange(float value)
    {
        min = max = value;
    }

    public FloatRange(float minValue, float maxValue)
    {
        min = minValue;
        max = maxValue;
    }

    public float Min => min;
    public float Max => max;

    public static FloatRange Zero => new FloatRange(0f, 0f);
    public static FloatRange One => new FloatRange(1f, 1f);
    public static FloatRange ZeroToOne => new FloatRange(0f, 1f);

    public FloatRange Normalized
    {
        get
        {
            if (min <= max)
                return this;
            return new FloatRange(max, min);
        }
    }

    public float Sample(System.Random random)
    {
        var normalized = Normalized;
        if (Mathf.Approximately(normalized.min, normalized.max))
            return normalized.min;

        float t = (float)random.NextDouble();
        return Mathf.Lerp(normalized.min, normalized.max, t);
    }
}