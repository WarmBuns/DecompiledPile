using RoR2;

namespace EntityStates.FalseSonBoss;

public class LunarGazeFirePlus : LunarGazeFire
{
	public static float plusDamageCoefficient;

	public override void OnEnter()
	{
		LunarGazeFire.playAttackSoundString = "Play_boss_falseson_skill3plus_lunarGaze_start";
		LunarGazeFire.playLoopSoundString = "Play_boss_falseson_skill3plus_lunarGaze_active_loop";
		LunarGazeFire.stopLoopSoundString = "Stop_boss_falseson_skill3plus_lunarGaze_active_loop";
		LunarGazeFire.isLunarGazePlusPlus = true;
		LunarGazeFire.damageCoefficient += plusDamageCoefficient;
		base.OnEnter();
	}

	public override void OnExit()
	{
		Util.PlaySound("Play_boss_falseson_skill3plus_lunarGaze_end", base.gameObject);
		base.OnExit();
	}
}
