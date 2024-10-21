using System.Collections.Generic;
using RoR2.Orbs;
using UnityEngine;

namespace RoR2.Items;

public class ShockNearbyBodyBehavior : BaseItemBodyBehavior
{
	private float teslaBuffRollTimer;

	private const float teslaRollInterval = 10f;

	private float teslaFireTimer;

	private float teslaResetListTimer;

	private float teslaResetListInterval = 0.5f;

	private const float teslaFireInterval = 1f / 12f;

	private bool teslaCrit;

	private bool teslaIsOn;

	private List<HealthComponent> previousTeslaTargetList = new List<HealthComponent>();

	private bool _grantingBuff;

	private BuffDef grantedBuff => RoR2Content.Buffs.TeslaField;

	private bool grantingBuff
	{
		get
		{
			return _grantingBuff;
		}
		set
		{
			if (_grantingBuff != value)
			{
				_grantingBuff = value;
				if (_grantingBuff)
				{
					base.body.AddBuff(grantedBuff);
				}
				else
				{
					base.body.RemoveBuff(grantedBuff);
				}
			}
		}
	}

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.ShockNearby;
	}

	private void FixedUpdate()
	{
		teslaBuffRollTimer += Time.fixedDeltaTime;
		if (teslaBuffRollTimer >= 10f)
		{
			teslaBuffRollTimer = 0f;
			teslaCrit = base.body.RollCrit();
			grantingBuff = !grantingBuff;
		}
		if (!grantingBuff)
		{
			return;
		}
		teslaFireTimer += Time.fixedDeltaTime;
		teslaResetListTimer += Time.fixedDeltaTime;
		if (teslaFireTimer >= 1f / 12f)
		{
			teslaFireTimer = 0f;
			LightningOrb lightningOrb = new LightningOrb
			{
				origin = base.body.corePosition,
				damageValue = base.body.damage * 2f,
				isCrit = teslaCrit,
				bouncesRemaining = 2 * stack,
				teamIndex = base.body.teamComponent.teamIndex,
				attacker = base.gameObject,
				procCoefficient = 0.3f,
				bouncedObjects = previousTeslaTargetList,
				lightningType = LightningOrb.LightningType.Tesla,
				damageColorIndex = DamageColorIndex.Item,
				range = 35f
			};
			HurtBox hurtBox = lightningOrb.PickNextTarget(base.transform.position);
			if ((bool)hurtBox)
			{
				previousTeslaTargetList.Add(hurtBox.healthComponent);
				lightningOrb.target = hurtBox;
				OrbManager.instance.AddOrb(lightningOrb);
			}
		}
		if (teslaResetListTimer >= teslaResetListInterval)
		{
			teslaResetListTimer -= teslaResetListInterval;
			previousTeslaTargetList.Clear();
		}
	}

	private void OnDisable()
	{
		if ((bool)base.body && base.body.HasBuff(grantedBuff))
		{
			base.body.RemoveBuff(grantedBuff);
		}
	}
}
