using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

public class ProjectileFireChildren : MonoBehaviour
{
	public float duration = 5f;

	public int count = 5;

	public GameObject childProjectilePrefab;

	private float timer;

	private float nextSpawnTimer;

	public float childDamageCoefficient = 1f;

	public float childProcCoefficient = 1f;

	private ProjectileDamage projectileDamage;

	private ProjectileController projectileController;

	public bool ignoreParentForChainController;

	private void Start()
	{
		projectileDamage = GetComponent<ProjectileDamage>();
		projectileController = GetComponent<ProjectileController>();
	}

	private void Update()
	{
		timer += Time.deltaTime;
		nextSpawnTimer += Time.deltaTime;
		if (timer >= duration)
		{
			Object.Destroy(base.gameObject);
		}
		if (nextSpawnTimer >= duration / (float)count)
		{
			nextSpawnTimer -= duration / (float)count;
			GameObject obj = Object.Instantiate(childProjectilePrefab, base.transform.position, Util.QuaternionSafeLookRotation(base.transform.forward));
			ProjectileController component = obj.GetComponent<ProjectileController>();
			if ((bool)component)
			{
				component.procChainMask = projectileController.procChainMask;
				component.procCoefficient = projectileController.procCoefficient * childProcCoefficient;
				component.Networkowner = projectileController.owner;
			}
			obj.GetComponent<TeamFilter>().teamIndex = GetComponent<TeamFilter>().teamIndex;
			ProjectileDamage component2 = obj.GetComponent<ProjectileDamage>();
			if ((bool)component2)
			{
				component2.damage = projectileDamage.damage * childDamageCoefficient;
				component2.crit = projectileDamage.crit;
				component2.force = projectileDamage.force;
				component2.damageColorIndex = projectileDamage.damageColorIndex;
			}
			NetworkServer.Spawn(obj);
		}
	}
}
