using RoR2.Orbs;
using UnityEngine;

namespace RoR2.Items;

public class SprintWispBodyBehavior : BaseItemBodyBehavior
{
	private static readonly float fireRate = 3f / 35f;

	private static readonly float searchRadius = 40f;

	private static readonly float damageCoefficient = 3f;

	private float fireTimer;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.SprintWisp;
	}

	private void FixedUpdate()
	{
		if (base.body.isSprinting)
		{
			fireTimer -= Time.fixedDeltaTime;
			if (fireTimer <= 0f && base.body.moveSpeed > 0f)
			{
				fireTimer += 1f / (fireRate * base.body.moveSpeed);
				Fire();
			}
		}
	}

	private void Fire()
	{
		DevilOrb devilOrb = new DevilOrb
		{
			origin = base.body.corePosition,
			damageValue = base.body.damage * damageCoefficient * (float)stack,
			teamIndex = base.body.teamComponent.teamIndex,
			attacker = base.gameObject,
			damageColorIndex = DamageColorIndex.Item,
			scale = 1f,
			effectType = DevilOrb.EffectType.Wisp,
			procCoefficient = 1f
		};
		if ((bool)(devilOrb.target = devilOrb.PickNextTarget(devilOrb.origin, searchRadius)))
		{
			devilOrb.isCrit = Util.CheckRoll(base.body.crit, base.body.master);
			OrbManager.instance.AddOrb(devilOrb);
		}
	}
}
