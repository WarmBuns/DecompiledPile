using RoR2;
using UnityEngine;

public class DestroyOnKill : MonoBehaviour, IOnKilledServerReceiver
{
	public GameObject effectPrefab;

	public void OnKilledServer(DamageReport damageReport)
	{
		Object.Instantiate(effectPrefab, base.transform.position, base.transform.rotation);
		Object.Destroy(base.gameObject);
	}
}
