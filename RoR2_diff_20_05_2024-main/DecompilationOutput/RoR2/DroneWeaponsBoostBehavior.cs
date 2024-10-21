using System.Collections.Generic;
using RoR2.Orbs;
using UnityEngine;

namespace RoR2;

public class DroneWeaponsBoostBehavior : CharacterBody.ItemBehavior
{
	private const string controllerPrefabPath = "Prefabs/NetworkedObjects/DroneWeaponsChainGunController";

	private const string missileMuzzleChildName = "MissileMuzzle";

	private const float microMissileDamageCoefficient = 3f;

	private const float microMissileProcCoefficient = 1f;

	private Transform missileMuzzleTransform;

	private GameObject chainGunController;

	public void Start()
	{
		GetComponent<InputBankTest>();
		ModelLocator component = GetComponent<ModelLocator>();
		if ((bool)component)
		{
			Transform modelTransform = component.modelTransform;
			if ((bool)modelTransform)
			{
				CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
				if ((bool)component2)
				{
					List<GameObject> itemDisplayObjects = component2.GetItemDisplayObjects(DLC1Content.Items.DroneWeaponsDisplay1.itemIndex);
					itemDisplayObjects.AddRange(component2.GetItemDisplayObjects(DLC1Content.Items.DroneWeaponsDisplay2.itemIndex));
					foreach (GameObject item in itemDisplayObjects)
					{
						ChildLocator component3 = item.GetComponent<ChildLocator>();
						if ((bool)component3)
						{
							Transform transform = component3.FindChild("MissileMuzzle");
							if ((bool)transform)
							{
								missileMuzzleTransform = transform;
								break;
							}
						}
					}
				}
			}
		}
		chainGunController = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DroneWeaponsChainGunController"));
		chainGunController.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject);
	}

	public void OnDestroy()
	{
		Object.Destroy(chainGunController);
	}

	public void OnEnemyHit(DamageInfo damageInfo, CharacterBody victimBody)
	{
		CharacterBody component = damageInfo.attacker.GetComponent<CharacterBody>();
		CharacterMaster characterMaster = component?.master;
		if ((bool)characterMaster && !damageInfo.procChainMask.HasProc(ProcType.MicroMissile) && Util.CheckRoll(10f, characterMaster))
		{
			ProcChainMask procChainMask = damageInfo.procChainMask;
			procChainMask.AddProc(ProcType.MicroMissile);
			HurtBox hurtBox = victimBody.mainHurtBox;
			if ((bool)victimBody.hurtBoxGroup)
			{
				hurtBox = victimBody.hurtBoxGroup.hurtBoxes[Random.Range(0, victimBody.hurtBoxGroup.hurtBoxes.Length)];
			}
			if ((bool)hurtBox)
			{
				MicroMissileOrb microMissileOrb = new MicroMissileOrb();
				microMissileOrb.damageValue = component.damage * 3f;
				microMissileOrb.isCrit = Util.CheckRoll(component.crit, characterMaster);
				microMissileOrb.teamIndex = TeamComponent.GetObjectTeam(component.gameObject);
				microMissileOrb.attacker = component.gameObject;
				microMissileOrb.procCoefficient = 1f;
				microMissileOrb.procChainMask = procChainMask;
				microMissileOrb.origin = (missileMuzzleTransform ? missileMuzzleTransform.position : component.corePosition);
				microMissileOrb.target = hurtBox;
				microMissileOrb.damageColorIndex = DamageColorIndex.Item;
				OrbManager.instance.AddOrb(microMissileOrb);
			}
		}
	}
}
