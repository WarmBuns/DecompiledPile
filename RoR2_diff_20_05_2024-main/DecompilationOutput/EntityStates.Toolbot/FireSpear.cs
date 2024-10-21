using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Toolbot;

public class FireSpear : GenericBulletBaseState, IToolbotPrimarySkillState, ISkillState
{
	public float charge;

	public static float recoilAmplitude;

	private static int FireSpearStateHash = Animator.StringToHash("FireSpear");

	private static int FireSpearParamHash = Animator.StringToHash("FireSpear.playbackRate");

	Transform IToolbotPrimarySkillState.muzzleTransform { get; set; }

	string IToolbotPrimarySkillState.baseMuzzleName => muzzleName;

	bool IToolbotPrimarySkillState.isInDualWield { get; set; }

	int IToolbotPrimarySkillState.currentHand { get; set; }

	string IToolbotPrimarySkillState.muzzleName { get; set; }

	public SkillDef skillDef { get; set; }

	public GenericSkill activatorSkillSlot { get; set; }

	public override void OnEnter()
	{
		BaseToolbotPrimarySkillStateMethods.OnEnter(this, base.gameObject, base.skillLocator, GetModelChildLocator());
		base.OnEnter();
	}

	protected override void ModifyBullet(BulletAttack bulletAttack)
	{
		base.ModifyBullet(bulletAttack);
		bulletAttack.stopperMask = LayerIndex.world.mask;
		bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
		bulletAttack.muzzleName = ((IToolbotPrimarySkillState)this).muzzleName;
	}

	protected override void FireBullet(Ray aimRay)
	{
		base.FireBullet(aimRay);
		base.characterBody.SetSpreadBloom(1f, canOnlyIncreaseBloom: false);
		AddRecoil(-0.6f * recoilAmplitude, -0.8f * recoilAmplitude, -0.1f * recoilAmplitude, 0.1f * recoilAmplitude);
		if (!((IToolbotPrimarySkillState)this).isInDualWield)
		{
			PlayAnimation("Gesture, Additive", FireSpearStateHash, FireSpearParamHash, duration);
		}
		else
		{
			BaseToolbotPrimarySkillStateMethods.PlayGenericFireAnim(this, base.gameObject, base.skillLocator, duration);
		}
	}

	public override void Update()
	{
		base.Update();
		base.characterBody.SetSpreadBloom(0.9f + base.age / duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new CooldownSpear
			{
				activatorSkillSlot = activatorSkillSlot
			});
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		this.Serialize(base.skillLocator, writer);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		this.Deserialize(base.skillLocator, reader);
	}
}
