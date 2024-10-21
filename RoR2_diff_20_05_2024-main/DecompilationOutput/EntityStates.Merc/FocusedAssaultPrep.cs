using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Merc;

public class FocusedAssaultPrep : BaseState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float smallHopVelocity;

	[SerializeField]
	public string enterSoundString;

	private float duration;

	private static int AssaulterPrepStateHash = Animator.StringToHash("AssaulterPrep");

	private static int AssaulterPrepParamHash = Animator.StringToHash("AssaulterPrep.playbackRate");

	public int dashIndex { private get; set; }

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("FullBody, Override", AssaulterPrepStateHash, AssaulterPrepParamHash, baseDuration);
		FindModelChildGameObject("PreDashEffect")?.SetActive(value: true);
		Util.PlaySound(enterSoundString, base.gameObject);
		Ray aimRay = GetAimRay();
		base.characterDirection.forward = aimRay.direction;
		base.characterDirection.moveVector = aimRay.direction;
		SmallHop(base.characterMotor, smallHopVelocity);
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
			base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.2f);
		}
		FindModelChildGameObject("PreDashEffect")?.SetActive(value: false);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new FocusedAssaultDash());
		}
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write((byte)dashIndex);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		dashIndex = reader.ReadByte();
	}
}
