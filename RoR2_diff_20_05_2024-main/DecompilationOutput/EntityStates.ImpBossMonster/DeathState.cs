using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ImpBossMonster;

public class DeathState : GenericCharacterDeath
{
	public static GameObject initialEffect;

	public static GameObject deathEffect;

	private static float duration = 3.3166666f;

	private float stopwatch;

	private Animator animator;

	private bool hasPlayedDeathEffect;

	private bool attemptedDeathBehavior;

	private EffectManagerHelper _emh_initialEffect;

	private MaterialPropertyBlock block;

	private static int DeathStateHash = Animator.StringToHash("Death");

	private static int DeathEffectParamHash = Animator.StringToHash("DeathEffect");

	public override void Reset()
	{
		base.Reset();
		stopwatch = 0f;
		animator = null;
		hasPlayedDeathEffect = false;
		attemptedDeathBehavior = false;
		_emh_initialEffect = null;
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		animator = GetModelAnimator();
		if ((bool)base.characterMotor)
		{
			base.characterMotor.enabled = false;
		}
		if ((bool)base.modelLocator)
		{
			Transform modelTransform = base.modelLocator.modelTransform;
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
			if ((bool)component)
			{
				component.FindChild("DustCenter").gameObject.SetActive(value: false);
				if ((bool)initialEffect)
				{
					EffectManager.SimpleMuzzleFlash(initialEffect, base.gameObject, "DeathCenter", transmit: false);
					if (EffectManager.ShouldUsePooledEffect(initialEffect))
					{
						_emh_initialEffect = EffectManager.LastSpawnedEffect;
					}
				}
			}
			if ((bool)component2)
			{
				for (int i = 0; i < component2.baseRendererInfos.Length; i++)
				{
					component2.baseRendererInfos[i].ignoreOverlays = true;
				}
			}
		}
		PlayAnimation("Fullbody Override", DeathStateHash);
	}

	public override void FixedUpdate()
	{
		if ((bool)animator)
		{
			stopwatch += GetDeltaTime();
			if (!hasPlayedDeathEffect && animator.GetFloat(DeathEffectParamHash) > 0.5f)
			{
				hasPlayedDeathEffect = true;
				EffectManager.SimpleMuzzleFlash(deathEffect, base.gameObject, "DeathCenter", transmit: false);
			}
			if (stopwatch >= duration)
			{
				AttemptDeathBehavior();
			}
		}
	}

	private void AttemptDeathBehavior()
	{
		if (attemptedDeathBehavior)
		{
			return;
		}
		attemptedDeathBehavior = true;
		if ((bool)base.modelLocator.modelBaseTransform)
		{
			EntityState.Destroy(base.modelLocator.modelBaseTransform.gameObject);
		}
		if (NetworkServer.active)
		{
			if (_emh_initialEffect != null && _emh_initialEffect.OwningPool != null)
			{
				_emh_initialEffect.OwningPool.ReturnObject(_emh_initialEffect);
				_emh_initialEffect = null;
			}
			EntityState.Destroy(base.gameObject);
		}
	}

	public override void OnExit()
	{
		if (!outer.destroying)
		{
			AttemptDeathBehavior();
		}
		base.OnExit();
	}
}
