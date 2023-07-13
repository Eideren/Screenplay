using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class SerializeReferenceButtonAttribute : PropertyAttribute
{
    public bool nicifyNames;

    public SerializeReferenceButtonAttribute(bool nicifyNamesParam = true)
    {
        nicifyNames = nicifyNamesParam;
    }
}