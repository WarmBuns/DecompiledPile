using RoR2;
using UnityEngine;

namespace EntityStates.GrandParentBoss;

public class SpawnState : GenericCharacterSpawnState
{
	public static GameObject preSpawnEffect;

	public static string preSpawnEffectMuzzle;

	public static float preSpawnDuration;

	public static GameObject spawnEffect;

	public static string spawnEffectMuzzle;

	public static Material spawnOverlayMaterial;

	public static float spawnOverlayDuration;

	public static float blastAttackRadius;

	public static float blastAttackForce;

	public static float blastAttackBonusForce;

	private bool hasSpawned;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	private bool preSpawnIsDone;

	private static int Spawn1StateHash = Animator.StringToHash("Spawn1");

	private static int Spawn1ParamHash = Animator.StringToHash("Spawn1.playbackRate");

	private bool isInvisible;

	public override void OnEnter()
	{
		base.OnEnter();
		characterModel = base.modelLocator.modelTransform.GetComponent<CharacterModel>();
		hurtboxGroup = base.modelLocator.modelTransform.GetComponent<HurtBoxGroup>();
		ToggleInvisibility(newInvisible: true);
		if ((bool)preSpawnEffect)
		{
			EffectManager.SimpleMuzzleFlash(preSpawnEffect, base.gameObject, preSpawnEffectMuzzle, transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!preSpawnIsDone && base.fixedAge > preSpawnDuration)
		{
			preSpawnIsDone = true;
			ToggleInvisibility(newInvisible: false);
			PlayAnimation("Body", Spawn1StateHash, Spawn1ParamHash, duration - preSpawnDuration);
			if ((bool)spawnEffect)
			{
				EffectManager.SimpleMuzzleFlash(spawnEffect, base.gameObject, spawnEffectMuzzle, transmit: false);
			}
			if (base.isAuthority)
			{
				BlastAttack obj = new BlastAttack
				{
					attacker = base.gameObject,
					inflictor = base.gameObject
				};
				obj.teamIndex = TeamComponent.GetObjectTeam(obj.attacker);
				obj.position = base.characterBody.corePosition;
				obj.procCoefficient = 1f;
				obj.radius = blastAttackRadius;
				obj.baseForce = blastAttackForce;
				obj.bonusForce = Vector3.up * blastAttackBonusForce;
				obj.baseDamage = 0f;
				obj.falloffModel = BlastAttack.FalloffModel.Linear;
				obj.damageColorIndex = DamageColorIndex.Item;
				obj.attackerFiltering = AttackerFiltering.NeverHitSelf;
				obj.Fire();
			}
			if ((bool)characterModel && (bool)spawnOverlayMaterial)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(characterModel.gameObject);
				temporaryOverlayInstance.duration = spawnOverlayDuration;
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = spawnOverlayMaterial;
				temporaryOverlayInstance.inspectorCharacterModel = characterModel;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.animateShaderAlpha = true;
			}
		}
	}

	private void ToggleInvisibility(bool newInvisible)
	{
		if (isInvisible != newInvisible)
		{
			isInvisible = newInvisible;
			if ((bool)characterModel)
			{
				characterModel.invisibilityCount += (isInvisible ? 1 : (-1));
			}
			if ((bool)hurtboxGroup)
			{
				hurtboxGroup.hurtBoxesDeactivatorCounter += (isInvisible ? 1 : (-1));
			}
		}
	}

	public override void OnExit()
	{
		ToggleInvisibility(newInvisible: false);
		base.OnExit();
	}
}
