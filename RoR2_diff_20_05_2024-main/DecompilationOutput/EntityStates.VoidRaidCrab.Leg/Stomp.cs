using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab.Leg;

public class Stomp : BaseStompState
{
	public static float blastDamageCoefficient;

	public static float blastRadius;

	public static float blastForce;

	public static GameObject blastEffectPrefab;

	private Vector3? previousToePosition;

	private bool hasDoneBlast;

	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextState(new PostStompReturnToBase
		{
			target = target
		});
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!hasDoneBlast && (bool)base.legController.mainBody && base.legController.mainBody.hasEffectiveAuthority)
		{
			TryStompCollisionAuthority();
		}
	}

	private void TryStompCollisionAuthority()
	{
		Vector3 currentToePosition = base.legController.toeTipTransform.position;
		TryAttack();
		previousToePosition = currentToePosition;
		void TryAttack()
		{
			if (previousToePosition.HasValue)
			{
				Vector3 value = previousToePosition.Value;
				if (!((currentToePosition - value).sqrMagnitude < 0.0025000002f) && Physics.Linecast(previousToePosition.Value, currentToePosition, out var hitInfo, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					DoBlastAuthority(hitInfo.point);
					base.legController.DoToeConcussionBlastAuthority(hitInfo.point, useEffect: false);
					hasDoneBlast = true;
				}
			}
		}
	}

	private void DoBlastAuthority(Vector3 blastOrigin)
	{
		CharacterBody mainBody = base.legController.mainBody;
		EffectData effectData = new EffectData();
		effectData.origin = blastOrigin;
		effectData.scale = blastRadius;
		effectData.rotation = Quaternion.LookRotation(Vector3.up);
		EffectManager.SpawnEffect(blastEffectPrefab, effectData, transmit: true);
		BlastAttack obj = new BlastAttack
		{
			attacker = mainBody.gameObject
		};
		obj.inflictor = obj.attacker;
		obj.baseDamage = blastDamageCoefficient * mainBody.damage;
		obj.baseForce = blastForce;
		obj.position = blastOrigin;
		obj.radius = blastRadius;
		obj.falloffModel = BlastAttack.FalloffModel.SweetSpot;
		obj.teamIndex = mainBody.teamComponent.teamIndex;
		obj.attackerFiltering = AttackerFiltering.NeverHitSelf;
		obj.damageType = DamageType.Generic;
		obj.crit = mainBody.RollCrit();
		obj.damageColorIndex = DamageColorIndex.Default;
		obj.impactEffect = EffectIndex.Invalid;
		obj.losType = BlastAttack.LoSType.None;
		obj.procChainMask = default(ProcChainMask);
		obj.procCoefficient = 1f;
		obj.Fire();
	}

	private void DoStompAttackAuthority(Vector3 hitPosition)
	{
		CharacterBody mainBody = base.legController.mainBody;
		if ((bool)mainBody)
		{
			Vector3 position = base.legController.footTranform.position;
			Vector3 position2 = base.legController.toeTipTransform.position;
			RaycastHit hitInfo;
			Vector3 vector = (Physics.Linecast(position, position2, out hitInfo, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? hitInfo.point : position2);
			EffectData effectData = new EffectData();
			effectData.origin = vector;
			effectData.scale = blastRadius;
			effectData.rotation = Quaternion.LookRotation(-hitInfo.normal);
			EffectManager.SpawnEffect(blastEffectPrefab, effectData, transmit: true);
			BlastAttack obj = new BlastAttack
			{
				attacker = mainBody.gameObject
			};
			obj.inflictor = obj.attacker;
			obj.baseDamage = blastDamageCoefficient * mainBody.damage;
			obj.baseForce = blastForce;
			obj.position = vector;
			obj.radius = blastRadius;
			obj.falloffModel = BlastAttack.FalloffModel.Linear;
			obj.teamIndex = mainBody.teamComponent.teamIndex;
			obj.attackerFiltering = AttackerFiltering.NeverHitSelf;
			obj.damageType = DamageType.Generic;
			obj.crit = mainBody.RollCrit();
			obj.damageColorIndex = DamageColorIndex.Default;
			obj.impactEffect = EffectIndex.Invalid;
			obj.losType = BlastAttack.LoSType.None;
			obj.procChainMask = default(ProcChainMask);
			obj.procCoefficient = 1f;
			obj.Fire();
		}
	}
}
