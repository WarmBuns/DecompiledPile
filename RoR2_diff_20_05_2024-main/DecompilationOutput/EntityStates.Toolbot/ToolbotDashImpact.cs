using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Toolbot;

public class ToolbotDashImpact : BaseState
{
	public HealthComponent victimHealthComponent;

	public Vector3 idealDirection;

	public float damageBoostFromSpeed;

	public bool isCrit;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			if ((bool)victimHealthComponent)
			{
				DamageInfo damageInfo = new DamageInfo
				{
					attacker = base.gameObject,
					damage = damageStat * ToolbotDash.knockbackDamageCoefficient * damageBoostFromSpeed,
					crit = isCrit,
					procCoefficient = 1f,
					damageColorIndex = DamageColorIndex.Item,
					damageType = DamageType.Stun1s,
					position = base.characterBody.corePosition
				};
				victimHealthComponent.TakeDamage(damageInfo);
				GlobalEventManager.instance.OnHitEnemy(damageInfo, victimHealthComponent.gameObject);
				GlobalEventManager.instance.OnHitAll(damageInfo, victimHealthComponent.gameObject);
			}
			base.healthComponent.TakeDamageForce(idealDirection * (0f - ToolbotDash.knockbackForce), alwaysApply: true);
		}
		if (base.isAuthority)
		{
			AddRecoil(-0.5f * ToolbotDash.recoilAmplitude * 3f, -0.5f * ToolbotDash.recoilAmplitude * 3f, -0.5f * ToolbotDash.recoilAmplitude * 8f, 0.5f * ToolbotDash.recoilAmplitude * 3f);
			EffectManager.SimpleImpactEffect(ToolbotDash.knockbackEffectPrefab, base.characterBody.corePosition, base.characterDirection.forward, transmit: true);
			outer.SetNextStateToMain();
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(victimHealthComponent ? victimHealthComponent.gameObject : null);
		writer.Write(idealDirection);
		writer.Write(damageBoostFromSpeed);
		writer.Write(isCrit);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		GameObject gameObject = reader.ReadGameObject();
		victimHealthComponent = (gameObject ? gameObject.GetComponent<HealthComponent>() : null);
		idealDirection = reader.ReadVector3();
		damageBoostFromSpeed = reader.ReadSingle();
		isCrit = reader.ReadBoolean();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
