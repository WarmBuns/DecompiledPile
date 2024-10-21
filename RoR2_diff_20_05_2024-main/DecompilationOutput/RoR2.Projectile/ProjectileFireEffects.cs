using UnityEngine;

namespace RoR2.Projectile;

public class ProjectileFireEffects : MonoBehaviour
{
	public float duration = 5f;

	public int count = 5;

	public GameObject effectPrefab;

	public Vector3 randomOffset;

	private float timer;

	private float nextSpawnTimer;

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
			if ((bool)effectPrefab)
			{
				Vector3 vector = new Vector3(Random.Range(0f - randomOffset.x, randomOffset.x), Random.Range(0f - randomOffset.y, randomOffset.y), Random.Range(0f - randomOffset.z, randomOffset.z));
				EffectManager.SimpleImpactEffect(effectPrefab, base.transform.position + vector, Vector3.forward, transmit: true);
			}
		}
	}
}
