using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using HG;
using JetBrains.Annotations;
using RoR2.EntitlementManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class Loadout
{
	public class BodyLoadoutManager
	{
		private sealed class BodyLoadout
		{
			public BodyIndex bodyIndex;

			public uint skinPreference;

			public uint[] skillPreferences;

			[NotNull]
			public BodyLoadout Clone()
			{
				return new BodyLoadout
				{
					bodyIndex = bodyIndex,
					skinPreference = skinPreference,
					skillPreferences = (uint[])skillPreferences.Clone()
				};
			}

			public bool ValueEquals(BodyLoadout other)
			{
				if (this == other)
				{
					return true;
				}
				if (other == null)
				{
					return false;
				}
				if (bodyIndex.Equals(other.bodyIndex))
				{
					if (skinPreference.Equals(other.skinPreference))
					{
						return ((IStructuralEquatable)skillPreferences).Equals((object)other.skillPreferences, (IEqualityComparer)EqualityComparer<uint>.Default);
					}
					return false;
				}
				return false;
			}

			public uint GetSkillVariant(int skillSlotIndex)
			{
				if ((uint)skillSlotIndex < skillPreferences.Length)
				{
					return skillPreferences[skillSlotIndex];
				}
				return 0u;
			}

			public bool SetSkillVariant(int skillSlotIndex, uint skillVariant)
			{
				if ((uint)skillSlotIndex < skillPreferences.Length)
				{
					skillPreferences[skillSlotIndex] = HGMath.Clamp(skillVariant, 0u, (uint)LookUpMaxSkillVariants(skillSlotIndex));
					return true;
				}
				return false;
			}

			private bool IsSkillVariantValid(int skillSlotIndex)
			{
				SkillFamily skillFamily = GetSkillFamily(skillSlotIndex);
				if (!skillFamily)
				{
					return false;
				}
				if (GetSkillVariant(skillSlotIndex) < skillFamily.variants.Length)
				{
					return true;
				}
				return false;
			}

			private bool IsSkillVariantLocked(int skillSlotIndex, UserProfile userProfile)
			{
				SkillFamily skillFamily = GetSkillFamily(skillSlotIndex);
				if (!skillFamily)
				{
					return false;
				}
				uint skillVariant = GetSkillVariant(skillSlotIndex);
				bool num = (bool)skillFamily.variants[skillVariant].unlockableDef && (bool)skillFamily.variants[skillVariant].unlockableDef.requiredExpansion;
				bool flag = false;
				if (num)
				{
					flag = EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(skillFamily.variants[skillVariant].unlockableDef.requiredExpansion.requiredEntitlement);
				}
				if (!num || flag)
				{
					return !userProfile.HasUnlockable(skillFamily.variants[skillVariant].unlockableDef);
				}
				return true;
			}

			private void ResetSkillVariant(int skillSlotIndex)
			{
				if ((uint)skillSlotIndex < skillPreferences.Length)
				{
					skillPreferences[skillSlotIndex] = GetSkillFamily(skillSlotIndex)?.defaultVariantIndex ?? 0;
				}
			}

			private bool IsSkinValid()
			{
				SkinDef[] bodySkins = BodyCatalog.GetBodySkins(bodyIndex);
				return skinPreference < bodySkins.Length;
			}

			private bool IsSkinLocked(UserProfile userProfile)
			{
				SkinDef safe = ArrayUtils.GetSafe(BodyCatalog.GetBodySkins(bodyIndex), (int)skinPreference);
				if (!safe)
				{
					return true;
				}
				bool num = (bool)safe.unlockableDef && (bool)safe.unlockableDef.requiredExpansion;
				bool flag = false;
				if (num)
				{
					EntitlementDef requiredEntitlement = safe.unlockableDef.requiredExpansion.requiredEntitlement;
					flag = EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(requiredEntitlement);
				}
				if (!num || flag)
				{
					return !userProfile.HasUnlockable(safe.unlockableDef);
				}
				return true;
			}

			private void ResetSkin()
			{
				skinPreference = 0u;
			}

			public void EnforceValidity()
			{
				for (int i = 0; i < skillPreferences.Length; i++)
				{
					if (!IsSkillVariantValid(i))
					{
						ResetSkillVariant(i);
					}
				}
				if (!IsSkinValid())
				{
					ResetSkin();
				}
			}

			public void EnforceUnlockables(UserProfile userProfile)
			{
				bool flag = false;
				for (int i = 0; i < skillPreferences.Length; i++)
				{
					if (IsSkillVariantLocked(i, userProfile))
					{
						ResetSkillVariant(i);
						flag = true;
					}
				}
				if (IsSkinLocked(userProfile))
				{
					ResetSkin();
					flag = true;
				}
				if (flag)
				{
					userProfile.OnLoadoutChanged();
				}
			}

			[CanBeNull]
			private SkillFamily GetSkillFamily(int skillSlotIndex)
			{
				return BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex)[skillSlotIndex].skillFamily;
			}

			public int LookUpMaxSkillVariants(int skillSlotIndex)
			{
				if ((uint)skillSlotIndex < skillPreferences.Length)
				{
					SkillFamily skillFamily = GetSkillFamily(skillSlotIndex);
					if ((object)skillFamily == null)
					{
						return 0;
					}
					return skillFamily.variants.Length;
				}
				return 0;
			}

			public void Serialize(NetworkWriter writer)
			{
				writer.WriteBodyIndex(bodyIndex);
				writer.WritePackedUInt32(skinPreference);
				for (int i = 0; i < skillPreferences.Length; i++)
				{
					writer.WritePackedUInt32(skillPreferences[i]);
				}
			}

			public void Deserialize(NetworkReader reader)
			{
				bodyIndex = reader.ReadBodyIndex();
				if (bodyIndex < (BodyIndex)0)
				{
					bodyIndex = (BodyIndex)0;
				}
				if ((int)bodyIndex >= BodyCatalog.bodyCount)
				{
					bodyIndex = (BodyIndex)(BodyCatalog.bodyCount - 1);
				}
				skinPreference = reader.ReadPackedUInt32();
				Array.Resize(ref skillPreferences, GetSkillSlotCountForBody(bodyIndex));
				for (int i = 0; i < skillPreferences.Length; i++)
				{
					SetSkillVariant(i, reader.ReadPackedUInt32());
				}
			}

			public XElement ToXml(string elementName)
			{
				XElement xElement = new XElement(elementName);
				xElement.SetAttributeValue("bodyName", BodyCatalog.GetBodyName(bodyIndex));
				xElement.Add(new XElement("Skin", skinPreference.ToString()));
				ref BodyInfo reference = ref allBodyInfos[(int)bodyIndex];
				for (int i = 0; i < reference.prefabSkillSlots.Length; i++)
				{
					int skillFamilyIndex = reference.skillFamilyIndices[i];
					SkillFamily skillFamily = SkillCatalog.GetSkillFamily(skillFamilyIndex);
					string skillFamilyName = SkillCatalog.GetSkillFamilyName(skillFamilyIndex);
					string variantName = skillFamily.GetVariantName((int)skillPreferences[i]);
					if (variantName != null)
					{
						XElement xElement2 = new XElement("SkillPreference", variantName);
						xElement2.SetAttributeValue("skillFamily", skillFamilyName);
						xElement.Add(xElement2);
					}
				}
				return xElement;
			}

			public bool FromXml(XElement element)
			{
				uint.TryParse(element.Element("Skin")?.Value ?? string.Empty, out skinPreference);
				string text = element.Attribute("bodyName")?.Value;
				if (text == null)
				{
					return false;
				}
				bodyIndex = BodyCatalog.FindBodyIndex(text);
				if (bodyIndex == BodyIndex.None)
				{
					Debug.LogFormat("Could not find body index for bodyName={0}", text);
					return false;
				}
				GenericSkill[] prefabSkillSlots = allBodyInfos[(int)bodyIndex].prefabSkillSlots;
				skillPreferences = new uint[prefabSkillSlots.Length];
				foreach (XElement item in element.Elements("SkillPreference"))
				{
					string text2 = item.Attribute("skillFamily")?.Value;
					string value = item.Value;
					if (text2 == null)
					{
						continue;
					}
					int num = FindSkillSlotIndex(text2);
					if (num != -1)
					{
						int variantIndex = prefabSkillSlots[num].skillFamily.GetVariantIndex(value);
						if (variantIndex != -1)
						{
							skillPreferences[num] = (uint)variantIndex;
							continue;
						}
						Debug.LogFormat("Could not find variant index for elementSkillFamilyName={0} elementSkillName={1}", text2, value);
					}
					else
					{
						Debug.LogFormat("Could not find skill slot index for elementSkillFamilyName={0} elementSkillName={1}", text2, value);
					}
				}
				return true;
				int FindSkillSlotIndex(string requestedSkillFamilyName)
				{
					for (int i = 0; i < prefabSkillSlots.Length; i++)
					{
						if (SkillCatalog.GetSkillFamilyName(prefabSkillSlots[i].skillFamily.catalogIndex).Equals(requestedSkillFamilyName, StringComparison.Ordinal))
						{
							return i;
						}
					}
					return -1;
				}
			}
		}

		private struct BodyInfo
		{
			public int[] skillFamilyIndices;

			public GenericSkill[] prefabSkillSlots;

			public int skillSlotCount => prefabSkillSlots.Length;
		}

		private BodyLoadout[] modifiedBodyLoadouts = Array.Empty<BodyLoadout>();

		private static BodyLoadout[] defaultBodyLoadouts;

		private static BodyInfo[] allBodyInfos;

		private int FindModifiedBodyLoadoutIndexByBodyIndex(BodyIndex bodyIndex)
		{
			for (int i = 0; i < modifiedBodyLoadouts.Length; i++)
			{
				if (modifiedBodyLoadouts[i].bodyIndex == bodyIndex)
				{
					return i;
				}
			}
			return -1;
		}

		private BodyLoadout GetReadOnlyBodyLoadout(BodyIndex bodyIndex)
		{
			int num = FindModifiedBodyLoadoutIndexByBodyIndex(bodyIndex);
			if (num == -1)
			{
				return defaultBodyLoadouts[(int)bodyIndex];
			}
			return modifiedBodyLoadouts[num];
		}

		private BodyLoadout GetOrCreateModifiedBodyLoadout(BodyIndex bodyIndex)
		{
			int num = FindModifiedBodyLoadoutIndexByBodyIndex(bodyIndex);
			if (num != -1)
			{
				return modifiedBodyLoadouts[num];
			}
			BodyLoadout value = GetDefaultLoadoutForBody(bodyIndex).Clone();
			ArrayUtils.ArrayAppend(ref modifiedBodyLoadouts, in value);
			return value;
		}

		public uint GetSkillVariant(BodyIndex bodyIndex, int skillSlot)
		{
			return GetReadOnlyBodyLoadout(bodyIndex).skillPreferences[skillSlot];
		}

		public void SetSkillVariant(BodyIndex bodyIndex, int skillSlot, uint skillVariant)
		{
			if ((uint)bodyIndex >= BodyCatalog.bodyCount)
			{
				throw new ArgumentOutOfRangeException("bodyIndex", (int)bodyIndex, $"Value provided for 'bodyIndex' is outside range [0, {(int)bodyIndex}]");
			}
			if (GetSkillVariant(bodyIndex, skillSlot) != skillVariant)
			{
				int skillSlotCountForBody = GetSkillSlotCountForBody(bodyIndex);
				if ((uint)skillSlot >= skillSlotCountForBody)
				{
					throw new ArgumentOutOfRangeException("skillSlot", skillSlot, $"Value provided for 'skillSlot' is outside range [0, {skillSlotCountForBody}) for body=[{(int)bodyIndex}]");
				}
				int num = allBodyInfos[(int)bodyIndex].prefabSkillSlots[skillSlot].skillFamily.variants.Length;
				if (skillVariant >= num)
				{
					throw new ArgumentOutOfRangeException("skillVariant", skillVariant, $"Value provided for 'skillVariant' is outside range [0, {num}) for body=[{(int)bodyIndex}] skillSlot=[{skillSlot}]");
				}
				BodyLoadout orCreateModifiedBodyLoadout = GetOrCreateModifiedBodyLoadout(bodyIndex);
				orCreateModifiedBodyLoadout.SetSkillVariant(skillSlot, skillVariant);
				RemoveBodyLoadoutIfDefault(orCreateModifiedBodyLoadout);
			}
		}

		public uint GetSkinIndex(BodyIndex bodyIndex)
		{
			return GetReadOnlyBodyLoadout(bodyIndex).skinPreference;
		}

		public void SetSkinIndex(BodyIndex bodyIndex, uint skinIndex)
		{
			BodyLoadout orCreateModifiedBodyLoadout = GetOrCreateModifiedBodyLoadout(bodyIndex);
			orCreateModifiedBodyLoadout.skinPreference = skinIndex;
			RemoveBodyLoadoutIfDefault(orCreateModifiedBodyLoadout);
		}

		private void RemoveBodyLoadoutIfDefault(BodyLoadout bodyLoadout)
		{
			RemoveBodyLoadoutIfDefault(FindModifiedBodyLoadoutIndexByBodyIndex(bodyLoadout.bodyIndex));
		}

		private void RemoveBodyLoadoutIfDefault(int modifiedBodyLoadoutIndex)
		{
			BodyLoadout obj = modifiedBodyLoadouts[modifiedBodyLoadoutIndex];
			if (obj.ValueEquals(GetDefaultLoadoutForBody(obj.bodyIndex)))
			{
				RemoveBodyLoadoutAt(modifiedBodyLoadoutIndex);
			}
		}

		private void RemoveBodyLoadoutAt(int i)
		{
			ArrayUtils.ArrayRemoveAtAndResize(ref modifiedBodyLoadouts, i);
		}

		private static BodyLoadout GetDefaultLoadoutForBody(BodyIndex bodyIndex)
		{
			return defaultBodyLoadouts[(int)bodyIndex];
		}

		private static int GetSkillSlotCountForBody(BodyIndex bodyIndex)
		{
			return allBodyInfos[(int)bodyIndex].skillSlotCount;
		}

		[SystemInitializer(new Type[]
		{
			typeof(SkillCatalog),
			typeof(BodyCatalog)
		})]
		private static void Init()
		{
			defaultBodyLoadouts = new BodyLoadout[BodyCatalog.bodyCount];
			allBodyInfos = new BodyInfo[defaultBodyLoadouts.Length];
			for (BodyIndex bodyIndex = (BodyIndex)0; (int)bodyIndex < defaultBodyLoadouts.Length; bodyIndex++)
			{
				BodyInfo bodyInfo = default(BodyInfo);
				bodyInfo.prefabSkillSlots = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex).GetComponents<GenericSkill>();
				bodyInfo.skillFamilyIndices = new int[bodyInfo.skillSlotCount];
				for (int i = 0; i < bodyInfo.prefabSkillSlots.Length; i++)
				{
					bodyInfo.skillFamilyIndices[i] = bodyInfo.prefabSkillSlots[i].skillFamily?.catalogIndex ?? (-1);
				}
				allBodyInfos[(int)bodyIndex] = bodyInfo;
				uint[] array = new uint[bodyInfo.skillSlotCount];
				for (int j = 0; j < bodyInfo.prefabSkillSlots.Length; j++)
				{
					array[j] = bodyInfo.prefabSkillSlots[j].skillFamily.defaultVariantIndex;
				}
				defaultBodyLoadouts[(int)bodyIndex] = new BodyLoadout
				{
					bodyIndex = bodyIndex,
					skinPreference = 0u,
					skillPreferences = array
				};
			}
			GenerateViewables();
		}

		public void Copy(BodyLoadoutManager dest)
		{
			Array.Resize(ref dest.modifiedBodyLoadouts, modifiedBodyLoadouts.Length);
			for (int i = 0; i < modifiedBodyLoadouts.Length; i++)
			{
				dest.modifiedBodyLoadouts[i] = modifiedBodyLoadouts[i].Clone();
			}
		}

		public void Clear()
		{
			modifiedBodyLoadouts = Array.Empty<BodyLoadout>();
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.WritePackedUInt32((uint)modifiedBodyLoadouts.Length);
			for (int i = 0; i < modifiedBodyLoadouts.Length; i++)
			{
				modifiedBodyLoadouts[i].Serialize(writer);
			}
		}

		public void Deserialize(NetworkReader reader)
		{
			try
			{
				int num = (int)reader.ReadPackedUInt32();
				if (num > BodyCatalog.bodyCount)
				{
					num = BodyCatalog.bodyCount;
				}
				Array.Resize(ref modifiedBodyLoadouts, num);
				for (int i = 0; i < num; i++)
				{
					BodyLoadout bodyLoadout = new BodyLoadout();
					bodyLoadout.Deserialize(reader);
					modifiedBodyLoadouts[i] = bodyLoadout;
				}
			}
			catch (Exception)
			{
				modifiedBodyLoadouts = Array.Empty<BodyLoadout>();
				throw;
			}
		}

		public bool ValueEquals(BodyLoadoutManager other)
		{
			if (this == other)
			{
				return true;
			}
			if (other == null)
			{
				return false;
			}
			if (modifiedBodyLoadouts.Length != other.modifiedBodyLoadouts.Length)
			{
				return false;
			}
			for (int i = 0; i < modifiedBodyLoadouts.Length; i++)
			{
				if (!modifiedBodyLoadouts[i].ValueEquals(other.modifiedBodyLoadouts[i]))
				{
					return false;
				}
			}
			return true;
		}

		public XElement ToXml(string elementName)
		{
			XElement xElement = new XElement(elementName);
			for (int i = 0; i < modifiedBodyLoadouts.Length; i++)
			{
				xElement.Add(modifiedBodyLoadouts[i].ToXml("BodyLoadout"));
			}
			return xElement;
		}

		public bool FromXml(XElement element)
		{
			List<BodyLoadout> bodyLoadouts = new List<BodyLoadout>();
			foreach (XElement item in element.Elements("BodyLoadout"))
			{
				BodyLoadout bodyLoadout = new BodyLoadout();
				if (bodyLoadout.FromXml(item) && !BodyLoadoutAlreadyDefined(bodyLoadout.bodyIndex) && !bodyLoadout.ValueEquals(GetDefaultLoadoutForBody(bodyLoadout.bodyIndex)))
				{
					bodyLoadouts.Add(bodyLoadout);
				}
			}
			modifiedBodyLoadouts = bodyLoadouts.ToArray();
			return true;
			bool BodyLoadoutAlreadyDefined(BodyIndex bodyIndex)
			{
				for (int i = 0; i < bodyLoadouts.Count; i++)
				{
					if (bodyLoadouts[i].bodyIndex == bodyIndex)
					{
						return true;
					}
				}
				return false;
			}
		}

		public void EnforceUnlockables(UserProfile userProfile)
		{
			for (int num = modifiedBodyLoadouts.Length - 1; num >= 0; num--)
			{
				modifiedBodyLoadouts[num].EnforceUnlockables(userProfile);
				RemoveBodyLoadoutIfDefault(num);
			}
		}

		public void EnforceValidity()
		{
			for (int num = modifiedBodyLoadouts.Length - 1; num >= 0; num--)
			{
				modifiedBodyLoadouts[num].EnforceValidity();
				RemoveBodyLoadoutIfDefault(num);
			}
		}
	}

	public readonly BodyLoadoutManager bodyLoadoutManager = new BodyLoadoutManager();

	private static readonly Stack<Loadout> instancePool = new Stack<Loadout>();

	public void Serialize(NetworkWriter writer)
	{
		bodyLoadoutManager.Serialize(writer);
	}

	public void Deserialize(NetworkReader reader)
	{
		bodyLoadoutManager.Deserialize(reader);
	}

	public void Copy(Loadout dest)
	{
		bodyLoadoutManager.Copy(dest.bodyLoadoutManager);
	}

	public void Clear()
	{
		bodyLoadoutManager.Clear();
	}

	public bool ValueEquals(Loadout other)
	{
		if (this == other)
		{
			return true;
		}
		if (other == null)
		{
			return false;
		}
		return bodyLoadoutManager.ValueEquals(other.bodyLoadoutManager);
	}

	public XElement ToXml(string elementName)
	{
		XElement xElement = new XElement(elementName);
		xElement.Add(bodyLoadoutManager.ToXml("BodyLoadouts"));
		return xElement;
	}

	public bool FromXml(XElement element)
	{
		bool flag = true;
		XElement xElement = element.Element("BodyLoadouts");
		flag = xElement != null && (flag & bodyLoadoutManager.FromXml(xElement));
		EnforceValidity();
		return flag;
	}

	public void EnforceValidity()
	{
		bodyLoadoutManager.EnforceValidity();
	}

	public void EnforceUnlockables(UserProfile userProfile)
	{
		bodyLoadoutManager.EnforceUnlockables(userProfile);
	}

	private static void GenerateViewables()
	{
		StringBuilder stringBuilder = new StringBuilder();
		ViewablesCatalog.Node node = new ViewablesCatalog.Node("Loadout", isFolder: true);
		ViewablesCatalog.Node parent = new ViewablesCatalog.Node("Bodies", isFolder: true, node);
		for (BodyIndex bodyIndex = (BodyIndex)0; (int)bodyIndex < BodyCatalog.bodyCount; bodyIndex++)
		{
			if (SurvivorCatalog.GetSurvivorIndexFromBodyIndex(bodyIndex) == SurvivorIndex.None)
			{
				continue;
			}
			string bodyName = BodyCatalog.GetBodyName(bodyIndex);
			GenericSkill[] bodyPrefabSkillSlots = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex);
			ViewablesCatalog.Node parent2 = new ViewablesCatalog.Node(bodyName, isFolder: true, parent);
			for (int i = 0; i < bodyPrefabSkillSlots.Length; i++)
			{
				SkillFamily skillFamily = bodyPrefabSkillSlots[i].skillFamily;
				if (skillFamily.variants.Length <= 1)
				{
					continue;
				}
				string skillFamilyName = SkillCatalog.GetSkillFamilyName(skillFamily.catalogIndex);
				for (uint num = 0u; num < skillFamily.variants.Length; num++)
				{
					ref SkillFamily.Variant reference = ref skillFamily.variants[num];
					UnlockableDef unlockableDef = reference.unlockableDef;
					string skillName = SkillCatalog.GetSkillName(reference.skillDef.skillIndex);
					stringBuilder.Append(skillFamilyName).Append(".").Append(skillName);
					string name = stringBuilder.ToString();
					stringBuilder.Clear();
					ViewablesCatalog.Node variantNode = new ViewablesCatalog.Node(name, isFolder: false, parent2);
					reference.viewableNode = variantNode;
					variantNode.shouldShowUnviewed = (UserProfile userProfile) => !userProfile.HasViewedViewable(variantNode.fullName) && userProfile.HasUnlockable(unlockableDef);
				}
			}
			SkinDef[] bodySkins = BodyCatalog.GetBodySkins(bodyIndex);
			if (bodySkins.Length <= 1)
			{
				continue;
			}
			ViewablesCatalog.Node parent3 = new ViewablesCatalog.Node("Skins", isFolder: true, parent2);
			for (int j = 0; j < bodySkins.Length; j++)
			{
				UnlockableDef unlockableDef2 = bodySkins[j].unlockableDef;
				if ((bool)unlockableDef2)
				{
					ViewablesCatalog.Node skinNode = new ViewablesCatalog.Node(bodySkins[j].name, isFolder: false, parent3);
					skinNode.shouldShowUnviewed = (UserProfile userProfile) => !userProfile.HasViewedViewable(skinNode.fullName) && userProfile.HasUnlockable(unlockableDef2);
				}
			}
		}
		ViewablesCatalog.AddNodeToRoot(node);
	}

	public static Loadout RequestInstance()
	{
		if (instancePool.Count == 0)
		{
			return new Loadout();
		}
		return instancePool.Pop();
	}

	public static Loadout ReturnInstance(Loadout instance)
	{
		instance.Clear();
		instancePool.Push(instance);
		return null;
	}

	[SystemInitializer(new Type[]
	{
		typeof(BodyCatalog),
		typeof(SkillCatalog),
		typeof(SkinCatalog)
	})]
	private static void Init()
	{
	}
}
