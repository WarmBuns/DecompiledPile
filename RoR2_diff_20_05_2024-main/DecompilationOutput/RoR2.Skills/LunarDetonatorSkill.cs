using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2.Skills;

public class LunarDetonatorSkill : SkillDef
{
	private class InstanceData : BaseSkillInstanceData
	{
		private GenericSkill _skillSlot;

		private LunarDetonatorPassiveAttachment lunarDetonatorPassiveAttachment;

		public GenericSkill skillSlot
		{
			get
			{
				return _skillSlot;
			}
			set
			{
				if ((object)_skillSlot == value)
				{
					return;
				}
				if ((object)_skillSlot != null)
				{
					_skillSlot.characterBody.onInventoryChanged -= OnInventoryChanged;
				}
				if ((bool)lunarDetonatorPassiveAttachment)
				{
					Object.Destroy(lunarDetonatorPassiveAttachment.gameObject);
				}
				lunarDetonatorPassiveAttachment = null;
				_skillSlot = value;
				if ((object)_skillSlot != null)
				{
					_skillSlot.characterBody.onInventoryChanged += OnInventoryChanged;
					if (NetworkServer.active && (bool)_skillSlot.characterBody)
					{
						GameObject gameObject = Object.Instantiate(lunarDetonatorPassiveAttachmentPrefab);
						lunarDetonatorPassiveAttachment = gameObject.GetComponent<LunarDetonatorPassiveAttachment>();
						lunarDetonatorPassiveAttachment.monitoredSkill = skillSlot;
						gameObject.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(_skillSlot.characterBody.gameObject);
					}
				}
			}
		}

		public void OnInventoryChanged()
		{
			skillSlot.RecalculateValues();
		}
	}

	private static GameObject lunarDetonatorPassiveAttachmentPrefab;

	[InitDuringStartup]
	private static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/NetworkedObjects/BodyAttachments/LunarDetonatorPassiveAttachment");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			lunarDetonatorPassiveAttachmentPrefab = x.Result;
		};
	}

	public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
	{
		return new InstanceData
		{
			skillSlot = skillSlot
		};
	}

	public override void OnUnassigned(GenericSkill skillSlot)
	{
		((InstanceData)skillSlot.skillInstanceData).skillSlot = null;
	}

	public override float GetRechargeInterval(GenericSkill skillSlot)
	{
		return (float)skillSlot.characterBody.inventory.GetItemCount(RoR2Content.Items.LunarSpecialReplacement) * baseRechargeInterval;
	}
}
