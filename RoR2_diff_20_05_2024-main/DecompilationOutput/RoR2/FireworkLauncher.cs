using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class FireworkLauncher : MonoBehaviour
{
	public GameObject projectilePrefab;

	public float launchInterval = 0.1f;

	public float damageCoefficient = 3f;

	public float coneAngle = 10f;

	public float randomCircleRange;

	[HideInInspector]
	public GameObject owner;

	[HideInInspector]
	public TeamIndex team;

	[HideInInspector]
	public int remaining;

	[HideInInspector]
	public bool crit;

	private float nextFireTimer;

	private void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if (remaining <= 0 || !owner)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		nextFireTimer -= Time.fixedDeltaTime;
		if (nextFireTimer <= 0f)
		{
			remaining--;
			nextFireTimer += launchInterval;
			FireMissile();
		}
	}

	private void FireMissile()
	{
		CharacterBody component = owner.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			ProcChainMask procChainMask = default(ProcChainMask);
			Vector2 vector = Random.insideUnitCircle * randomCircleRange;
			MissileUtils.FireMissile(base.transform.position + new Vector3(vector.x, 0f, vector.y), component, procChainMask, null, component.damage * damageCoefficient, crit, projectilePrefab, DamageColorIndex.Item, addMissileProc: false);
		}
	}
}
