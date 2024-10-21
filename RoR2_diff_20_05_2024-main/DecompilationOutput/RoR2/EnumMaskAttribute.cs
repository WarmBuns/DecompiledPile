using System;
using UnityEngine;

namespace RoR2;

public class EnumMaskAttribute : PropertyAttribute
{
	public Type enumType;

	public EnumMaskAttribute(Type enumType)
	{
		this.enumType = enumType;
	}
}
