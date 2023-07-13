using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class SerializeReferenceUIRestrictionExcludeTypes : PropertyAttribute
{
    public readonly Type[] Types;

    public SerializeReferenceUIRestrictionExcludeTypes(params Type[] types)
    {
        Types = types;
    }
}