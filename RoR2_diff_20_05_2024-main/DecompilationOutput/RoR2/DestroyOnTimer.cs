using UnityEngine;

namespace RoR2;

public class DestroyOnTimer : MonoBehaviour
{
	public float duration;

	public bool resetAgeOnDisable;

	private float age;

	private EffectManagerHelper efh;

	private void Start()
	{
		if (!efh)
		{
			efh = GetComponent<EffectManagerHelper>();
		}
	}

	private void FixedUpdate()
	{
		age += Time.fixedDeltaTime;
		if (age > duration)
		{
			if ((bool)efh && efh.OwningPool != null)
			{
				efh.OwningPool.ReturnObject(efh);
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}
	}

	private void OnDisable()
	{
		if (resetAgeOnDisable || ((bool)efh && efh.OwningPool != null))
		{
			age = 0f;
		}
	}
}
