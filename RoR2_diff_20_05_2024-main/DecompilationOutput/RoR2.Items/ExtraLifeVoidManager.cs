using System;
using UnityEngine;

namespace RoR2.Items;

public static class ExtraLifeVoidManager
{
	private const ulong seedSalt = 733uL;

	private static Xoroshiro128Plus rng;

	private static string[] voidBodyNames;

	public static GameObject rezEffectPrefab { get; private set; }

	[SystemInitializer(new Type[] { typeof(ItemCatalog) })]
	private static void Init()
	{
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/Effects/VoidRezEffect", delegate(GameObject operationResult)
		{
			rezEffectPrefab = operationResult;
		});
		voidBodyNames = new string[1] { "NullifierBody" };
		Run.onRunStartGlobal += OnRunStart;
	}

	private static void OnRunStart(Run run)
	{
		rng = new Xoroshiro128Plus(run.seed ^ 0x2DD);
	}

	public static GameObject GetNextBodyPrefab()
	{
		return BodyCatalog.FindBodyPrefab(voidBodyNames[rng.RangeInt(0, voidBodyNames.Length)]);
	}
}
