using System;
using System.Collections.Generic;
using RoR2.Skills;
using UnityEngine;

namespace RoR2;

[Serializable]
public class SerializableLoadout
{
	[Serializable]
	public struct BodyLoadout
	{
		[Serializable]
		public struct SkillChoice
		{
			public SkillFamily skillFamily;

			public SkillDef variant;
		}

		public CharacterBody body;

		public SkillChoice[] skillChoices;

		public SkinDef skinChoice;
	}

	private class LoadoutBuilder
	{
		private struct SkillSetter
		{
			public readonly BodyIndex bodyIndex;

			public readonly int skillSlotIndex;

			public readonly uint skillVariantIndex;

			public SkillSetter(BodyIndex bodyIndex, int skillSlotIndex, uint skillVariantIndex)
			{
				this.bodyIndex = bodyIndex;
				this.skillSlotIndex = skillSlotIndex;
				this.skillVariantIndex = skillVariantIndex;
			}
		}

		private struct SkinSetter
		{
			public readonly BodyIndex bodyIndex;

			public readonly uint skinIndex;

			public SkinSetter(BodyIndex bodyIndex, uint skinIndex)
			{
				this.bodyIndex = bodyIndex;
				this.skinIndex = skinIndex;
			}
		}

		private readonly SkillSetter[] skillSetters;

		private readonly SkinSetter[] skinSetters;

		public LoadoutBuilder(SerializableLoadout serializedLoadout)
		{
			BodyLoadout[] bodyLoadouts = serializedLoadout.bodyLoadouts;
			List<SkillSetter> list = new List<SkillSetter>(8);
			List<SkinSetter> list2 = new List<SkinSetter>(bodyLoadouts.Length);
			for (int i = 0; i < bodyLoadouts.Length; i++)
			{
				ref BodyLoadout reference = ref bodyLoadouts[i];
				CharacterBody body = reference.body;
				if (!body)
				{
					continue;
				}
				BodyIndex bodyIndex = body.bodyIndex;
				GenericSkill[] bodyPrefabSkillSlots = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex);
				BodyLoadout.SkillChoice[] skillChoices = reference.skillChoices;
				for (int j = 0; j < skillChoices.Length; j++)
				{
					ref BodyLoadout.SkillChoice reference2 = ref skillChoices[j];
					int num = FindSkillSlotIndex(bodyPrefabSkillSlots, reference2.skillFamily);
					int num2 = FindSkillVariantIndex(reference2.skillFamily, reference2.variant);
					if (num != -1 && num2 != -1)
					{
						list.Add(new SkillSetter(bodyIndex, num, (uint)num2));
					}
				}
				int num3 = Array.IndexOf(BodyCatalog.GetBodySkins(bodyIndex), reference.skinChoice);
				if (num3 != -1)
				{
					list2.Add(new SkinSetter(bodyIndex, (uint)num3));
				}
			}
			skillSetters = list.ToArray();
			skinSetters = list2.ToArray();
		}

		public void Apply(Loadout loadout)
		{
			for (int i = 0; i < skillSetters.Length; i++)
			{
				ref SkillSetter reference = ref skillSetters[i];
				loadout.bodyLoadoutManager.SetSkillVariant(reference.bodyIndex, reference.skillSlotIndex, reference.skillVariantIndex);
			}
			for (int j = 0; j < skinSetters.Length; j++)
			{
				ref SkinSetter reference2 = ref skinSetters[j];
				loadout.bodyLoadoutManager.SetSkinIndex(reference2.bodyIndex, reference2.skinIndex);
			}
		}

		private static int FindSkillSlotIndex(GenericSkill[] skillSlots, SkillFamily skillFamily)
		{
			for (int i = 0; i < skillSlots.Length; i++)
			{
				if ((object)skillSlots[i].skillFamily == skillFamily)
				{
					return i;
				}
			}
			return -1;
		}

		private static int FindSkillVariantIndex(SkillFamily skillFamily, SkillDef skillDef)
		{
			if ((bool)skillFamily)
			{
				if (skillFamily.variants != null)
				{
					for (int i = 0; i < skillFamily.variants.Length; i++)
					{
						if ((object)skillFamily.variants[i].skillDef == skillDef)
						{
							return i;
						}
					}
				}
				else
				{
					Debug.LogError("SkillFamily " + ((UnityEngine.Object)skillFamily).name + " has null skill variants");
				}
			}
			else
			{
				Debug.LogError("FindSkillVariantIndex: SkillFamily is null");
			}
			return -1;
		}
	}

	public BodyLoadout[] bodyLoadouts = Array.Empty<BodyLoadout>();

	private LoadoutBuilder loadoutBuilder;

	private static bool loadoutSystemReady;

	public bool isEmpty => bodyLoadouts.Length == 0;

	public void Apply(Loadout loadout)
	{
		if (!loadoutSystemReady)
		{
			throw new InvalidOperationException("Loadout system is not yet initialized.");
		}
		if (loadoutBuilder == null)
		{
			loadoutBuilder = new LoadoutBuilder(this);
		}
		loadoutBuilder.Apply(loadout);
	}

	[SystemInitializer(new Type[] { typeof(Loadout) })]
	private static void Init()
	{
		loadoutSystemReady = true;
	}
}
