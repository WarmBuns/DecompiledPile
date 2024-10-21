using RoR2;
using UnityEngine;

namespace EntityStates.AurelioniteHeart;

public class AurelioniteHeartActivationState : AurelioniteHeartEntityState
{
	public static float expGrantingDuration = 3f;

	[SerializeField]
	public static float itemGrantingDuration = 10f;

	[SerializeField]
	public ItemTier rewardDisplayTier;

	[SerializeField]
	[Tooltip("The prefab to use for the reward pickup.")]
	public GameObject rewardPickupPrefab;

	[SerializeField]
	public Vector3 rewardOffset;

	[SerializeField]
	public string falseSonSurvivorAchievement;

	[SerializeField]
	public ItemDef halcyonSeed;

	[SerializeField]
	public int rewardOptionCount = 1;

	[SerializeField]
	public PickupDropTable rewardDropTable;

	public bool rewardGiven;

	public override void OnEnter()
	{
		base.OnEnter();
		rewardGiven = false;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!heartController.activated || !(base.fixedAge >= expGrantingDuration))
		{
			return;
		}
		int participatingPlayerCount = Run.instance.participatingPlayerCount;
		if (base.gameObject == null || rewardDropTable == null || participatingPlayerCount == 0)
		{
			return;
		}
		if (participatingPlayerCount > 0 && (bool)base.gameObject && (bool)rewardDropTable && !rewardGiven)
		{
			rewardGiven = true;
			int num = participatingPlayerCount;
			float angle = 360f / (float)num;
			Vector3 vector = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			Vector3 position = base.gameObject.transform.position + rewardOffset;
			int num2 = 0;
			while (num2 < num)
			{
				GenericPickupController.CreatePickupInfo pickupInfo = default(GenericPickupController.CreatePickupInfo);
				pickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(rewardDisplayTier);
				pickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(rewardOptionCount, rewardDropTable, rng);
				pickupInfo.rotation = Quaternion.identity;
				pickupInfo.prefabOverride = rewardPickupPrefab;
				PickupDropletController.CreatePickupDroplet(pickupInfo, position, vector);
				num2++;
				vector = quaternion * vector;
			}
		}
		if (base.fixedAge >= itemGrantingDuration)
		{
			if (FalseSonUnlockStateMet())
			{
				outer.SetNextState(new AurelioniteHeartFalseSonSurvivorUnlockState());
			}
			else
			{
				outer.SetNextState(new AurelioniteHeartInertState());
			}
		}
	}

	private bool FalseSonUnlockStateMet()
	{
		bool result = false;
		int num = 0;
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if (!instance.networkUser.localUser.userProfile.HasAchievement("UnlockFalseSon"))
			{
				num++;
			}
		}
		if (num > 0)
		{
			foreach (PlayerCharacterMasterController instance2 in PlayerCharacterMasterController.instances)
			{
				if (instance2.master.inventory.GetItemCount(RoR2Content.Items.TitanGoldDuringTP) > 0 && num > 0)
				{
					result = true;
					GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/DelusionItemDissolveVFX");
					EffectData effectData = new EffectData
					{
						origin = instance2.master.GetBody().transform.position,
						genericFloat = 1.5f,
						genericUInt = (uint)(halcyonSeed.itemIndex + 1)
					};
					effectData.SetNetworkedObjectReference(base.gameObject);
					EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
					instance2.master.inventory.RemoveItem(halcyonSeed);
					FalseSonUnlocked();
				}
			}
		}
		return result;
	}
}
