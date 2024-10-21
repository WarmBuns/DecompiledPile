using System;
using JetBrains.Annotations;

namespace RoR2;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[MeansImplicitUse]
public class AssetCheckAttribute : Attribute
{
	public Type assetType;

	public AssetCheckAttribute(Type assetType)
	{
		this.assetType = assetType;
	}
}
