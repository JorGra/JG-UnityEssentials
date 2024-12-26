using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

[Serializable]
public class InterfaceReference<TInterface, TObject> where TObject : Object where TInterface : class
{
    [SerializeField, HideInInspector] TObject underlyingValue;

    public TInterface Value
    {
        get => underlyingValue switch
        {
            null => null,
            TInterface @interface => @interface,
            _ => throw new InvalidOperationException($"{underlyingValue} needs to implement interface {nameof(TInterface)}.")
        };
        set => underlyingValue = value switch { 
            null => null,
            TObject newValue => newValue,
            _ => throw new ArgumentException($"{value} needs to be of type {nameof(TObject)}.", string.Empty)
        };
    }

    public TObject UnderlyingValue
    {
        get => underlyingValue;
        set => underlyingValue = value;
    }

    public InterfaceReference()
    {
    
    }

    public InterfaceReference(TObject target)
    {
        underlyingValue = target;
    }

    public InterfaceReference(TInterface target)
    {
        underlyingValue = target as TObject;
    }
}

[Serializable]
public class InterfaceReference<TInterface> : InterfaceReference<TInterface, Object> where TInterface : class
{

}