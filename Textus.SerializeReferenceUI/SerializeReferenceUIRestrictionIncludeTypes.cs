using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class SerializeReferenceUIRestrictionIncludeTypes : PropertyAttribute
{
    public readonly Type[] Types;

    public SerializeReferenceUIRestrictionIncludeTypes(params Type[] types)
    {
        Types = types;
    }
}