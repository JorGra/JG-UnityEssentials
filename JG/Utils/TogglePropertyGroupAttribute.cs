using System;
using UnityEngine;


/// <summary>
/// Draws a boolean toggle that can gate a set of sibling serialized properties.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class TogglePropertyGroupAttribute : PropertyAttribute
{
    public string GroupLabel { get; }

    /// <param name="groupLabel">Optional label shown next to the toggle.</param>
    public TogglePropertyGroupAttribute(string groupLabel = null)
    {
        GroupLabel = groupLabel;
    }
}

/// <summary>
/// Marks a property as part of a toggle group controlled by a sibling boolean field.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class ToggleGroupMemberAttribute : PropertyAttribute
{
    public string ToggleProperty { get; }
    public int IndentLevel { get; }

    /// <param name="toggleProperty">Name of the boolean field that controls this property.</param>
    /// <param name="indentLevel">Relative indent applied when the property is visible.</param>
    public ToggleGroupMemberAttribute(string toggleProperty, int indentLevel = 1)
    {
        ToggleProperty = toggleProperty;
        IndentLevel = Mathf.Max(0, indentLevel);
    }
}