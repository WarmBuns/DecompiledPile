using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using RoR2.ContentManagement;
using RoR2.Modding;

namespace RoR2;

public static class BuffCatalog
{
	private static BuffDef[] buffDefs;

	public static BuffIndex[] eliteBuffIndices;

	public static BuffIndex[] debuffBuffIndices;

	public static BuffIndex[] nonHiddenBuffIndices;

	public static BuffIndex[] ignoreGrowthNectarIndices;

	private static readonly Dictionary<string, BuffIndex> nameToBuffIndex = new Dictionary<string, BuffIndex>();

	[Obsolete("Use IContentPackProvider instead.")]
	public static readonly CatalogModHelperProxy<BuffDef> modHelper = new CatalogModHelperProxy<BuffDef>("RoR2.BuffCatalog.modHelper", LegacyModContentPackProvider.instance.registrationContentPack.buffDefs);

	public static int buffCount => buffDefs.Length;

	private static void RegisterBuff(BuffIndex buffIndex, BuffDef buffDef)
	{
		buffDef.buffIndex = buffIndex;
		nameToBuffIndex[buffDef.name] = buffIndex;
	}

	public static BuffDef GetBuffDef(BuffIndex buffIndex)
	{
		return ArrayUtils.GetSafe(buffDefs, (int)buffIndex);
	}

	public static BuffIndex FindBuffIndex(string buffName)
	{
		if (nameToBuffIndex.TryGetValue(buffName, out var value))
		{
			return value;
		}
		return BuffIndex.None;
	}

	public static T[] GetPerBuffBuffer<T>()
	{
		return new T[buffCount];
	}

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		SetBuffDefs(ContentManager.buffDefs);
	}

	private static void SetBuffDefs(BuffDef[] newBuffDefs)
	{
		nameToBuffIndex.Clear();
		buffDefs = ArrayUtils.Clone(newBuffDefs);
		for (BuffIndex buffIndex = (BuffIndex)0; (int)buffIndex < buffDefs.Length; buffIndex++)
		{
			RegisterBuff(buffIndex, buffDefs[(int)buffIndex]);
		}
		eliteBuffIndices = (from buffDef in buffDefs
			where buffDef.isElite
			select buffDef.buffIndex).ToArray();
		debuffBuffIndices = (from buffDef in buffDefs
			where buffDef.isDebuff
			select buffDef.buffIndex).ToArray();
		nonHiddenBuffIndices = (from buffDef in buffDefs
			where !buffDef.isHidden
			select buffDef.buffIndex).ToArray();
		ignoreGrowthNectarIndices = (from buffDef in buffDefs
			where buffDef.ignoreGrowthNectar
			select buffDef.buffIndex).ToArray();
	}
}
