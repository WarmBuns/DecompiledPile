using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.QuestVolatileBattery;

public class CountDown : QuestVolatileBatteryBaseState
{
	public static float duration;

	public static GameObject vfxPrefab;

	public static float explosionRadius;

	public static GameObject explosionEffectPrefab;

	private GameObject[] vfxInstances = Array.Empty<GameObject>();

	private bool detonated;

	public override void OnEnter()
	{
		base.OnEnter();
		if (!vfxPrefab || !base.attachedCharacterModel)
		{
			return;
		}
		List<GameObject> equipmentDisplayObjects = base.attachedCharacterModel.GetEquipmentDisplayObjects(RoR2Content.Equipment.QuestVolatileBattery.equipmentIndex);
		if (equipmentDisplayObjects.Count > 0)
		{
			vfxInstances = new GameObject[equipmentDisplayObjects.Count];
			for (int i = 0; i < vfxInstances.Length; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(vfxPrefab, equipmentDisplayObjects[i].transform);
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
				vfxInstances[i] = gameObject;
			}
		}
	}

	public override void OnExit()
	{
		GameObject[] array = vfxInstances;
		for (int i = 0; i < array.Length; i++)
		{
			EntityState.Destroy(array[i]);
		}
		vfxInstances = Array.Empty<GameObject>();
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	private void FixedUpdateServer()
	{
		if (base.fixedAge >= duration && !detonated)
		{
			detonated = true;
			Detonate();
		}
	}

	public void Detonate()
	{
		if ((bool)base.networkedBodyAttachment.attachedBody)
		{
			Vector3 corePosition = base.networkedBodyAttachment.attachedBody.corePosition;
			float baseDamage = 0f;
			if ((bool)base.attachedHealthComponent)
			{
				baseDamage = base.attachedHealthComponent.fullCombinedHealth * 3f;
			}
			EffectManager.SpawnEffect(explosionEffectPrefab, new EffectData
			{
				origin = corePosition,
				scale = explosionRadius
			}, transmit: true);
			BlastAttack blastAttack = new BlastAttack();
			blastAttack.position = corePosition + UnityEngine.Random.onUnitSphere;
			blastAttack.radius = explosionRadius;
			blastAttack.falloffModel = BlastAttack.FalloffModel.None;
			blastAttack.attacker = base.networkedBodyAttachment.attachedBodyObject;
			blastAttack.inflictor = base.networkedBodyAttachment.gameObject;
			blastAttack.damageColorIndex = DamageColorIndex.Item;
			blastAttack.baseDamage = baseDamage;
			blastAttack.baseForce = 5000f;
			blastAttack.bonusForce = Vector3.zero;
			blastAttack.attackerFiltering = AttackerFiltering.AlwaysHit;
			blastAttack.crit = false;
			blastAttack.procChainMask = default(ProcChainMask);
			blastAttack.procCoefficient = 0f;
			blastAttack.teamIndex = base.networkedBodyAttachment.attachedBody.teamComponent.teamIndex;
			blastAttack.Fire();
			base.networkedBodyAttachment.attachedBody.inventory.SetEquipmentIndex(EquipmentIndex.None);
			outer.SetNextState(new Idle());
		}
	}
}
