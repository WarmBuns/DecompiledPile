using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HG;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

public static class SkinCatalog
{
	private static SkinDef[] allSkinDefs = Array.Empty<SkinDef>();

	private static SkinDef[][] skinsByBody = Array.Empty<SkinDef[]>();

	public static int skinCount => allSkinDefs.Length;

	[SystemInitializer(new Type[] { typeof(BodyCatalog) })]
	private static void Init()
	{
		List<SkinDef> list = new List<SkinDef>();
		skinsByBody = new SkinDef[BodyCatalog.bodyCount][];
		for (BodyIndex bodyIndex = (BodyIndex)0; (int)bodyIndex < BodyCatalog.bodyCount; bodyIndex++)
		{
			SkinDef[] array = ArrayUtils.Clone(FindSkinsForBody(bodyIndex));
			skinsByBody[(int)bodyIndex] = array;
			list.AddRange(array);
		}
		allSkinDefs = list.ToArray();
		for (int i = 0; i < allSkinDefs.Length; i++)
		{
			allSkinDefs[i].skinIndex = (SkinIndex)i;
		}
	}

	[CanBeNull]
	public static SkinDef FindCurrentSkinDefForBodyInstance(GameObject bodyObject)
	{
		ModelLocator component = bodyObject.GetComponent<ModelLocator>();
		if (!component || !component.modelTransform)
		{
			return null;
		}
		ModelSkinController component2 = component.modelTransform.GetComponent<ModelSkinController>();
		if (!component2)
		{
			return null;
		}
		return ArrayUtils.GetSafe(component2.skins, component2.currentSkinIndex);
	}

	[NotNull]
	private static SkinDef[] FindSkinsForBody(BodyIndex bodyIndex)
	{
		ModelLocator component = BodyCatalog.GetBodyPrefab(bodyIndex).GetComponent<ModelLocator>();
		if (!component || !component.modelTransform)
		{
			return Array.Empty<SkinDef>();
		}
		ModelSkinController component2 = component.modelTransform.GetComponent<ModelSkinController>();
		if (!component2)
		{
			return Array.Empty<SkinDef>();
		}
		return component2.skins;
	}

	[CanBeNull]
	public static SkinDef GetSkinDef(SkinIndex skinIndex)
	{
		return ArrayUtils.GetSafe(allSkinDefs, (int)skinIndex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NotNull]
	private static SkinDef[] GetBodySkinDefs(BodyIndex bodyIndex)
	{
		return ArrayUtils.GetSafe(skinsByBody, (int)bodyIndex, Array.Empty<SkinDef>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetBodySkinCount(BodyIndex bodyIndex)
	{
		return GetBodySkinDefs(bodyIndex).Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SkinDef GetBodySkinDef(BodyIndex bodyIndex, int skinIndex)
	{
		return ArrayUtils.GetSafe(GetBodySkinDefs(bodyIndex), skinIndex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindLocalSkinIndexForBody(BodyIndex bodyIndex, [CanBeNull] SkinDef skinDef)
	{
		return Array.IndexOf(GetBodySkinDefs(bodyIndex), skinDef);
	}
}
