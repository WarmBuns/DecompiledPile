using System;
using UnityEngine;

namespace RoR2;

public class TypeRestrictedReferenceAttribute : PropertyAttribute
{
	public readonly Type[] allowedTypes;

	public TypeRestrictedReferenceAttribute(params Type[] allowedTypes)
	{
		if (allowedTypes != null)
		{
			this.allowedTypes = allowedTypes;
		}
		else
		{
			this.allowedTypes = Array.Empty<Type>();
		}
	}
}
