using System;

namespace RoR2.ContentManagement;

public class TargetAssetNameAttribute : Attribute
{
	public readonly string targetAssetName;

	public TargetAssetNameAttribute(string targetAssetName)
	{
		this.targetAssetName = targetAssetName;
	}
}
