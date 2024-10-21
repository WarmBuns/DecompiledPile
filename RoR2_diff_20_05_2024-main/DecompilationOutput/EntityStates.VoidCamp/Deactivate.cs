using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidCamp;

public class Deactivate : EntityState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public string onEnterSoundString;

	[SerializeField]
	public string baseAnimationLayerName;

	[SerializeField]
	public string baseAnimationStateName;

	[SerializeField]
	public string deactivateChildName;

	[SerializeField]
	public string additiveAnimationLayerName;

	[SerializeField]
	public string additiveAnimationStateName;

	[SerializeField]
	public string completeObjectiveChatMessageToken;

	[SerializeField]
	public PickupDropTable rewardDropTable;

	[SerializeField]
	public GameObject rewardPickupPrefab;

	[SerializeField]
	public bool dropItem;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(additiveAnimationLayerName, additiveAnimationStateName);
		FogDamageController componentInChildren = outer.GetComponentInChildren<FogDamageController>();
		if ((bool)componentInChildren)
		{
			componentInChildren.enabled = false;
		}
		Util.PlaySound(onEnterSoundString, base.gameObject);
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator)
		{
			modelChildLocator.FindChild("ActiveFX").gameObject.SetActive(value: false);
			modelChildLocator.FindChild("RangeFX").GetComponent<AnimateShaderAlpha>().enabled = true;
		}
		Chat.SendBroadcastChat(new Chat.SimpleChatMessage
		{
			baseToken = completeObjectiveChatMessageToken
		});
		if (!NetworkServer.active || !dropItem)
		{
			return;
		}
		Transform transform = modelChildLocator.FindChild("RewardSpawnTarget");
		int participatingPlayerCount = Run.instance.participatingPlayerCount;
		if (participatingPlayerCount > 0 && (bool)transform && (bool)rewardDropTable)
		{
			int num = participatingPlayerCount;
			float angle = 360f / (float)num;
			Vector3 vector = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			Vector3 position = transform.transform.position;
			PickupPickerController.Option[] pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, rewardDropTable, Run.instance.treasureRng);
			int num2 = 0;
			while (num2 < num)
			{
				GenericPickupController.CreatePickupInfo pickupInfo = default(GenericPickupController.CreatePickupInfo);
				pickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.VoidTier2);
				pickupInfo.pickerOptions = pickerOptions;
				pickupInfo.rotation = Quaternion.identity;
				pickupInfo.prefabOverride = rewardPickupPrefab;
				pickupInfo.position = position;
				PickupDropletController.CreatePickupDroplet(pickupInfo, position, vector);
				num2++;
				vector = quaternion * vector;
			}
		}
	}

	public override void OnExit()
	{
		PlayAnimation(baseAnimationLayerName, baseAnimationStateName);
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator)
		{
			modelChildLocator.FindChildGameObject(deactivateChildName).gameObject.SetActive(value: false);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new EntityStates.Idle());
		}
	}
}
