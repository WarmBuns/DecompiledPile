using RoR2;
using UnityEngine;

namespace EntityStates.CaptainSupplyDrop;

public class ShockZoneMainState : BaseMainState
{
	public static GameObject shockEffectPrefab;

	public static float shockRadius;

	public static float shockDamageCoefficient;

	public static float shockFrequency;

	private float shockTimer;

	protected override Interactability GetInteractability(Interactor activator)
	{
		return Interactability.Disabled;
	}

	public override void OnEnter()
	{
		base.OnEnter();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		shockTimer += GetDeltaTime();
		if (shockTimer > 1f / shockFrequency)
		{
			shockTimer -= 1f / shockFrequency;
			Shock();
		}
	}

	private void Shock()
	{
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.radius = shockRadius;
		blastAttack.baseDamage = 0f;
		blastAttack.damageType = DamageType.Silent | DamageType.Shock5s;
		blastAttack.falloffModel = BlastAttack.FalloffModel.None;
		blastAttack.attacker = null;
		blastAttack.teamIndex = teamFilter.teamIndex;
		blastAttack.position = base.transform.position;
		blastAttack.Fire();
		if ((bool)shockEffectPrefab)
		{
			EffectManager.SpawnEffect(shockEffectPrefab, new EffectData
			{
				origin = base.transform.position,
				scale = shockRadius
			}, transmit: false);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}
