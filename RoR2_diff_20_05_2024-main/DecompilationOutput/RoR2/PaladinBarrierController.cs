using RoR2.Orbs;
using UnityEngine;

namespace RoR2;

public class PaladinBarrierController : MonoBehaviour, IBarrier
{
	public float blockLaserDamageCoefficient;

	public float blockLaserProcCoefficient;

	public float blockLaserDistance;

	private float totalDamageBlocked;

	private CharacterBody characterBody;

	private InputBankTest inputBank;

	private TeamComponent teamComponent;

	private bool barrierIsOn;

	public Transform barrierPivotTransform;

	public void BlockedDamage(DamageInfo damageInfo, float actualDamageBlocked)
	{
		totalDamageBlocked += actualDamageBlocked;
		LightningOrb lightningOrb = new LightningOrb();
		lightningOrb.teamIndex = teamComponent.teamIndex;
		lightningOrb.origin = damageInfo.position;
		lightningOrb.damageValue = actualDamageBlocked * blockLaserDamageCoefficient;
		lightningOrb.bouncesRemaining = 0;
		lightningOrb.attacker = damageInfo.attacker;
		lightningOrb.procCoefficient = blockLaserProcCoefficient;
		lightningOrb.lightningType = LightningOrb.LightningType.TreePoisonDart;
		HurtBox hurtBox = lightningOrb.PickNextTarget(lightningOrb.origin);
		if ((bool)hurtBox)
		{
			lightningOrb.target = hurtBox;
			lightningOrb.isCrit = Util.CheckRoll(characterBody.crit, characterBody.master);
			OrbManager.instance.AddOrb(lightningOrb);
		}
	}

	public void EnableBarrier()
	{
		barrierPivotTransform.gameObject.SetActive(value: true);
		barrierIsOn = true;
	}

	public void DisableBarrier()
	{
		barrierPivotTransform.gameObject.SetActive(value: false);
		barrierIsOn = false;
	}

	private void Start()
	{
		inputBank = GetComponent<InputBankTest>();
		characterBody = GetComponent<CharacterBody>();
		teamComponent = GetComponent<TeamComponent>();
		DisableBarrier();
	}

	private void Update()
	{
		if (barrierIsOn)
		{
			barrierPivotTransform.rotation = Util.QuaternionSafeLookRotation(inputBank.aimDirection);
		}
	}
}
