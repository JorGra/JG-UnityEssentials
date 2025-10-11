#if UNITY_EDITOR
using Gameplay.Planets;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JG.Editor
{
    /// <summary>
    /// Custom inspector drawer enabling ranged float editing with a slider.
    /// </summary>
    [CustomPropertyDrawer(typeof(FloatRange))]
    public sealed class FloatRangePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty minProp = property.FindPropertyRelative("min");
            SerializedProperty maxProp = property.FindPropertyRelative("max");

            float minValue = minProp.floatValue;
            float maxValue = maxProp.floatValue;

            MinMaxSliderAttribute sliderAttr = null;
            if (fieldInfo != null)
                sliderAttr = fieldInfo.GetCustomAttribute<MinMaxSliderAttribute>();

            float minLimit = sliderAttr != null ? sliderAttr.MinLimit : 0f;
            float maxLimit = sliderAttr != null ? sliderAttr.MaxLimit : 1f;

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, label);
            const float fieldWidth = 50f;
            float sliderWidth = Mathf.Max(10f, position.width - (fieldWidth * 2f) - 10f);

            Rect minRect = new Rect(position.x, position.y, fieldWidth, position.height);
            Rect sliderRect = new Rect(minRect.xMax + 5f, position.y, sliderWidth, position.height);
            Rect maxRect = new Rect(sliderRect.xMax + 5f, position.y, fieldWidth, position.height);

            EditorGUI.BeginChangeCheck();
            float sliderMin = minValue;
            float sliderMax = maxValue;
            var minFieldContent = new GUIContent(string.Empty, "Minimum value for this range.");
            var maxFieldContent = new GUIContent(string.Empty, "Maximum value for this range.");
            var sliderContent = new GUIContent(string.Empty, $"Drag to set values between {minLimit:0.##} and {maxLimit:0.##}.");

            EditorGUI.MinMaxSlider(sliderRect, sliderContent, ref sliderMin, ref sliderMax, minLimit, maxLimit);

            minValue = sliderMin;
            maxValue = sliderMax;

            minValue = EditorGUI.FloatField(minRect, minFieldContent, minValue);
            maxValue = EditorGUI.FloatField(maxRect, maxFieldContent, maxValue);

            if (EditorGUI.EndChangeCheck())
            {
                minValue = Mathf.Clamp(minValue, minLimit, maxLimit);
                maxValue = Mathf.Clamp(maxValue, minLimit, maxLimit);
                if (maxValue < minValue)
                    maxValue = minValue;

                minProp.floatValue = minValue;
                maxProp.floatValue = maxValue;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif
