using UnityEngine;

namespace RoR2.Projectile;

public class ProjectileGhostCluster : MonoBehaviour
{
	public GameObject ghostClusterPrefab;

	public int clusterCount;

	public bool distributeEvenly;

	public float clusterDistance;

	private void Start()
	{
		float num = 1f / (Mathf.Log(clusterCount, 4f) + 1f);
		Vector3 position = base.transform.position;
		for (int i = 0; i < clusterCount; i++)
		{
			GameObject obj = Object.Instantiate(position: position + ((!distributeEvenly) ? (Random.insideUnitSphere * clusterDistance) : Vector3.zero), original: ghostClusterPrefab, rotation: Quaternion.identity, parent: base.transform);
			obj.transform.localScale = Vector3.one / (Mathf.Log(clusterCount, 4f) + 1f);
			TrailRenderer component = obj.GetComponent<TrailRenderer>();
			if ((bool)component)
			{
				component.widthMultiplier *= num;
			}
		}
	}

	private void Update()
	{
	}
}
