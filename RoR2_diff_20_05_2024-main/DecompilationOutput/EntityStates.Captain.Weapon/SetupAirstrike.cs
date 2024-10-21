using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Captain.Weapon;

public class SetupAirstrike : BaseState
{
	public static SkillDef primarySkillDef;

	public static GameObject crosshairOverridePrefab;

	public static string enterSoundString;

	public static string exitSoundString;

	public static GameObject effectMuzzlePrefab;

	public static string effectMuzzleString;

	public static float baseExitDuration;

	private GenericSkill primarySkillSlot;

	private GameObject effectMuzzleInstance;

	private Animator modelAnimator;

	private float timerSinceComplete;

	private bool beginExit;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private static int PrepAirstrikeHash = Animator.StringToHash("PrepAirstrike");

	private float exitDuration => baseExitDuration / attackSpeedStat;

	public override void OnEnter()
	{
		base.OnEnter();
		primarySkillSlot = (base.skillLocator ? base.skillLocator.primary : null);
		if ((bool)primarySkillSlot)
		{
			primarySkillSlot.SetSkillOverride(this, primarySkillDef, GenericSkill.SkillOverridePriority.Contextual);
		}
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.SetBool(PrepAirstrikeHash, value: true);
		}
		PlayCrossfade("Gesture, Override", PrepAirstrikeHash, 0.1f);
		PlayCrossfade("Gesture, Additive", PrepAirstrikeHash, 0.1f);
		Transform transform = FindModelChild(effectMuzzleString);
		if ((bool)transform)
		{
			effectMuzzleInstance = Object.Instantiate(effectMuzzlePrefab, transform);
		}
		if ((bool)crosshairOverridePrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
		}
		Util.PlaySound(enterSoundString, base.gameObject);
		Util.PlaySound("Play_captain_shift_active_loop", base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = GetAimRay().direction;
		}
		if (!primarySkillSlot || primarySkillSlot.stock == 0)
		{
			beginExit = true;
		}
		if (beginExit)
		{
			timerSinceComplete += GetDeltaTime();
			if (timerSinceComplete > exitDuration)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)primarySkillSlot)
		{
			primarySkillSlot.UnsetSkillOverride(this, primarySkillDef, GenericSkill.SkillOverridePriority.Contextual);
		}
		Util.PlaySound(exitSoundString, base.gameObject);
		Util.PlaySound("Stop_captain_shift_active_loop", base.gameObject);
		if ((bool)effectMuzzleInstance)
		{
			EntityState.Destroy(effectMuzzleInstance);
		}
		if ((bool)modelAnimator)
		{
			modelAnimator.SetBool(PrepAirstrikeHash, value: false);
		}
		crosshairOverrideRequest?.Dispose();
		base.OnExit();
	}
}
