using System;
using UnityEngine;

namespace RoR2;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class ConditionalHideAttribute : PropertyAttribute
{
	public enum DisablingType
	{
		ReadOnly = 2,
		DontDraw
	}

	public string comparedPropertyName { get; private set; }

	public object comparedValue { get; private set; }

	public DisablingType disablingType { get; private set; }

	public ConditionalHideAttribute(string comparedPropertyName, object comparedValue, DisablingType disablingType = DisablingType.DontDraw)
	{
		this.comparedPropertyName = comparedPropertyName;
		this.comparedValue = comparedValue;
		this.disablingType = disablingType;
	}
}
