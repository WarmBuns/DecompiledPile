using RoR2;

namespace EntityStates.GoldGat;

public class GoldGatIdle : BaseGoldGatState
{
	public static string windDownSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(windDownSoundString, base.gameObject);
	}

	public override void Update()
	{
		base.Update();
		if ((bool)gunAnimator)
		{
			gunAnimator.SetFloat("Crank.playbackRate", 0f, 1f, GetDeltaTime());
		}
		if (base.isAuthority && shouldFire && bodyMaster.money != 0 && bodyEquipmentSlot.stock > 0)
		{
			outer.SetNextState(new GoldGatFire
			{
				shouldFire = shouldFire
			});
		}
	}
}
