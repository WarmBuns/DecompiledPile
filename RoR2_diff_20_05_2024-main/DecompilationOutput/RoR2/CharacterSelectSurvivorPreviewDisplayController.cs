using System;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class CharacterSelectSurvivorPreviewDisplayController : MonoBehaviour
{
	[Serializable]
	public struct SkillChangeResponse
	{
		public SkillFamily triggerSkillFamily;

		public SkillDef triggerSkill;

		public UnityEvent response;
	}

	[Serializable]
	public struct SkinChangeResponse
	{
		public SkinDef triggerSkin;

		public UnityEvent response;
	}

	public GameObject bodyPrefab;

	public SkillChangeResponse[] skillChangeResponses;

	public SkinChangeResponse[] skinChangeResponses;

	private Loadout currentLoadout;

	public NetworkUser networkUser { get; set; }

	private void OnEnable()
	{
		currentLoadout = Loadout.RequestInstance();
		NetworkUser.onLoadoutChangedGlobal += OnLoadoutChangedGlobal;
		RoR2Application.onNextUpdate += Refresh;
		RunDefaultResponses();
	}

	private void OnDisable()
	{
		NetworkUser.onLoadoutChangedGlobal -= OnLoadoutChangedGlobal;
		currentLoadout = Loadout.ReturnInstance(currentLoadout);
	}

	private static int FindSkillSlotIndex(BodyIndex bodyIndex, SkillFamily skillFamily)
	{
		GenericSkill[] bodyPrefabSkillSlots = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex);
		for (int i = 0; i < bodyPrefabSkillSlots.Length; i++)
		{
			if (bodyPrefabSkillSlots[i].skillFamily == skillFamily)
			{
				return i;
			}
		}
		return -1;
	}

	private static int FindVariantIndex(SkillFamily skillFamily, SkillDef skillDef)
	{
		SkillFamily.Variant[] variants = skillFamily.variants;
		for (int i = 0; i < variants.Length; i++)
		{
			if (variants[i].skillDef == skillDef)
			{
				return i;
			}
		}
		return -1;
	}

	private static bool HasSkillVariantEnabled(Loadout loadout, BodyIndex bodyIndex, SkillFamily skillFamily, SkillDef skillDef)
	{
		int num = FindSkillSlotIndex(bodyIndex, skillFamily);
		int num2 = FindVariantIndex(skillFamily, skillDef);
		if (num == -1 || num2 == -1)
		{
			return false;
		}
		return loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, num) == num2;
	}

	private void Refresh()
	{
		if ((bool)this && (bool)networkUser)
		{
			OnLoadoutChangedGlobal(networkUser);
		}
	}

	private void OnLoadoutChangedGlobal(NetworkUser changedNetworkUser)
	{
		if (changedNetworkUser != networkUser)
		{
			return;
		}
		Loadout loadout = Loadout.RequestInstance();
		changedNetworkUser.networkLoadout.CopyLoadout(loadout);
		BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(bodyPrefab);
		if (bodyIndex == BodyIndex.None)
		{
			return;
		}
		SkillChangeResponse[] array = skillChangeResponses;
		for (int i = 0; i < array.Length; i++)
		{
			SkillChangeResponse skillChangeResponse = array[i];
			bool num = HasSkillVariantEnabled(currentLoadout, bodyIndex, skillChangeResponse.triggerSkillFamily, skillChangeResponse.triggerSkill);
			bool flag = HasSkillVariantEnabled(loadout, bodyIndex, skillChangeResponse.triggerSkillFamily, skillChangeResponse.triggerSkill);
			if (!num && flag)
			{
				skillChangeResponse.response?.Invoke();
			}
		}
		SkinChangeResponse[] array2 = skinChangeResponses;
		for (int i = 0; i < array2.Length; i++)
		{
			SkinChangeResponse skinChangeResponse = array2[i];
			uint num2 = (uint)SkinCatalog.FindLocalSkinIndexForBody(bodyIndex, skinChangeResponse.triggerSkin);
			uint skinIndex = currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndex);
			uint skinIndex2 = loadout.bodyLoadoutManager.GetSkinIndex(bodyIndex);
			if (skinIndex != skinIndex2 && skinIndex2 == num2)
			{
				skinChangeResponse.response?.Invoke();
			}
		}
		loadout.Copy(currentLoadout);
		Loadout.ReturnInstance(loadout);
	}

	private void RunDefaultResponses()
	{
		BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(bodyPrefab);
		if (bodyIndex == BodyIndex.None)
		{
			return;
		}
		SkillChangeResponse[] array = skillChangeResponses;
		for (int i = 0; i < array.Length; i++)
		{
			SkillChangeResponse skillChangeResponse = array[i];
			if (HasSkillVariantEnabled(currentLoadout, bodyIndex, skillChangeResponse.triggerSkillFamily, skillChangeResponse.triggerSkill))
			{
				skillChangeResponse.response?.Invoke();
			}
		}
		SkinChangeResponse[] array2 = skinChangeResponses;
		for (int i = 0; i < array2.Length; i++)
		{
			SkinChangeResponse skinChangeResponse = array2[i];
			uint num = (uint)SkinCatalog.FindLocalSkinIndexForBody(bodyIndex, skinChangeResponse.triggerSkin);
			if (currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndex) == num)
			{
				skinChangeResponse.response?.Invoke();
			}
		}
	}
}
