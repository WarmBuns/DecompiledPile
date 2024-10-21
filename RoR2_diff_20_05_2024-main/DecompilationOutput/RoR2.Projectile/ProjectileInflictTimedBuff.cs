using UnityEngine;

namespace RoR2.Projectile;

public class ProjectileInflictTimedBuff : MonoBehaviour, IOnDamageInflictedServerReceiver
{
	public BuffDef buffDef;

	public float duration;

	public void OnDamageInflictedServer(DamageReport damageReport)
	{
		CharacterBody victimBody = damageReport.victimBody;
		if ((bool)victimBody)
		{
			victimBody.AddTimedBuff(buffDef.buffIndex, duration);
		}
	}

	private void OnValidate()
	{
		if (!buffDef)
		{
			Debug.LogWarningFormat(this, "ProjectileInflictTimedBuff {0} has no buff specified.", this);
		}
	}
}
