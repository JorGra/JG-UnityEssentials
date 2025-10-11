using System;
using UnityEngine;

namespace JG.Editor
{
    /// <summary>
    /// Attribute enabling a min/max slider in the inspector for fields storing range values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinMaxSliderAttribute : PropertyAttribute
    {
        public float MinLimit { get; }
        public float MaxLimit { get; }

        public MinMaxSliderAttribute(float minLimit, float maxLimit)
        {
            MinLimit = minLimit;
            MaxLimit = maxLimit;
        }
    }
}
