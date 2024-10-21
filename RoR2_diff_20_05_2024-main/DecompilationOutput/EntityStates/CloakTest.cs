using RoR2;
using UnityEngine.Networking;

namespace EntityStates;

public class CloakTest : BaseState
{
	private float duration = 3f;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterBody && NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.Cloak);
			base.characterBody.AddBuff(RoR2Content.Buffs.CloakSpeed);
		}
	}

	public override void OnExit()
	{
		if ((bool)base.characterBody && NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.Cloak);
			base.characterBody.RemoveBuff(RoR2Content.Buffs.CloakSpeed);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}
