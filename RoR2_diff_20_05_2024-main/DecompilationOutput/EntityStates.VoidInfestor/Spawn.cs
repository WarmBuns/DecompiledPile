using RoR2;
using UnityEngine;

namespace EntityStates.VoidInfestor;

public class Spawn : BaseState
{
	public static GameObject spawnEffectPrefab;

	public static float velocityStrength;

	public static float spread;

	private static int SpawnStateHash = Animator.StringToHash("Spawn");

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)spawnEffectPrefab)
		{
			EffectManager.SimpleImpactEffect(spawnEffectPrefab, base.characterBody.corePosition, Vector3.up, transmit: false);
		}
		if ((bool)base.characterMotor)
		{
			Vector3 vector = (Vector3.up + Random.onUnitSphere * spread) * velocityStrength;
			base.characterMotor.ApplyForce(vector, alwaysApply: true, disableAirControlUntilCollision: true);
			base.characterDirection.forward = vector;
		}
		PlayAnimation("Base", SpawnStateHash);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && (bool)base.characterMotor && base.characterMotor.isGrounded && base.fixedAge > 0.1f)
		{
			outer.SetNextStateToMain();
		}
	}
}
