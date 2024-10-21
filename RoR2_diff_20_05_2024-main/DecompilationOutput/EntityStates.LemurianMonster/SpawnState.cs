using RoR2;

namespace EntityStates.LemurianMonster;

public class SpawnState : EntityState
{
	public static float duration = 2f;

	public static string spawnSoundString;

	public static string devotionHissSoundString;

	public static string devotionSpawnSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		CharacterBody component = base.gameObject.GetComponent<CharacterBody>();
		if ((component.bodyFlags & CharacterBody.BodyFlags.Devotion) != 0 && component.master.devotionInventoryPrefab != null)
		{
			Util.PlaySound(devotionHissSoundString, base.gameObject);
			Util.PlaySound(devotionSpawnSoundString, base.gameObject);
		}
		else
		{
			Util.PlaySound(spawnSoundString, base.gameObject);
		}
		PlayAnimation("Body", "Spawn1", "Spawn1.playbackRate", duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}
