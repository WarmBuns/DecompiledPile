using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.CaptainSupplyDrop;

public class HitGroundState : BaseCaptainSupplyDropState
{
	public static float baseDuration;

	public static GameObject effectPrefab;

	public static float impactBulletDistance;

	public static float impactBulletRadius;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		if (NetworkServer.active)
		{
			GameObject ownerObject = GetComponent<GenericOwnership>().ownerObject;
			ProjectileDamage component = GetComponent<ProjectileDamage>();
			Vector3 position = base.transform.position;
			Vector3 vector = -base.transform.up;
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.origin = position - vector * impactBulletDistance;
			bulletAttack.aimVector = vector;
			bulletAttack.maxDistance = impactBulletDistance + 1f;
			bulletAttack.stopperMask = default(LayerMask);
			bulletAttack.hitMask = LayerIndex.CommonMasks.bullet;
			bulletAttack.damage = component.damage;
			bulletAttack.damageColorIndex = component.damageColorIndex;
			bulletAttack.damageType = component.damageType;
			bulletAttack.bulletCount = 1u;
			bulletAttack.minSpread = 0f;
			bulletAttack.maxSpread = 0f;
			bulletAttack.owner = ownerObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.procCoefficient = 0f;
			bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
			bulletAttack.isCrit = RollCrit();
			bulletAttack.smartCollision = false;
			bulletAttack.sniper = false;
			bulletAttack.force = component.force;
			bulletAttack.radius = impactBulletRadius;
			bulletAttack.hitEffectPrefab = effectPrefab;
			bulletAttack.Fire();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new DeployState());
		}
	}
}
