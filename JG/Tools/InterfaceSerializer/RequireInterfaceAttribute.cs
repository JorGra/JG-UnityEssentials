using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


[AttributeUsage(AttributeTargets.Field)]
public class RequireInterfaceAttribute : PropertyAttribute{

    public readonly Type InterfaceType;

    public RequireInterfaceAttribute(Type interfaceType)
    {
        Debug.Assert(interfaceType.IsInterface, $"{nameof(interfaceType)} needs to be an interface.");
        InterfaceType = interfaceType;
    }
}