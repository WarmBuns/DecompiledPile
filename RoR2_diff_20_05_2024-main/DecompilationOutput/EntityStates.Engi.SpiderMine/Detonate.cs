using System.Collections.Generic;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.SpiderMine;

public class Detonate : BaseSpiderMineState
{
	public static float blastRadius;

	public static GameObject blastEffectPrefab;

	[SerializeField]
	private List<string> childLocatorStringToDisable = new List<string> { "Armed", "Chase", "PreDetonate" };

	protected override bool shouldStick => false;

	public override void OnEnter()
	{
		base.OnEnter();
		if (!NetworkServer.active)
		{
			return;
		}
		ProjectileDamage component = GetComponent<ProjectileDamage>();
		Vector3 position = base.transform.position;
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.position = position;
		blastAttack.attacker = base.projectileController.owner;
		blastAttack.baseDamage = component.damage;
		blastAttack.baseForce = component.force;
		blastAttack.bonusForce = Vector3.zero;
		blastAttack.crit = component.crit;
		blastAttack.damageColorIndex = component.damageColorIndex;
		blastAttack.damageType = component.damageType;
		blastAttack.falloffModel = BlastAttack.FalloffModel.None;
		blastAttack.inflictor = base.gameObject;
		blastAttack.procChainMask = base.projectileController.procChainMask;
		blastAttack.radius = blastRadius;
		blastAttack.teamIndex = base.projectileController.teamFilter.teamIndex;
		blastAttack.procCoefficient = base.projectileController.procCoefficient;
		blastAttack.Fire();
		EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
		{
			origin = position,
			scale = blastRadius
		}, transmit: true);
		float duration = 0.25f;
		PlayAnimation("Base", BaseSpiderMineState.IdleToArmedStateHash, BaseSpiderMineState.IdleToArmedParamHash, duration);
		foreach (string item in childLocatorStringToDisable)
		{
			Transform transform = FindModelChild(item);
			if ((bool)transform)
			{
				transform.gameObject.SetActive(value: false);
			}
		}
		EntityState.Destroy(base.gameObject);
	}
}
