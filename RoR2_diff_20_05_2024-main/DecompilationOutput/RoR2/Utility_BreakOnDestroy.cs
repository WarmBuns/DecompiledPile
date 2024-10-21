using UnityEngine;

namespace RoR2;

public class Utility_BreakOnDestroy : MonoBehaviour
{
	private void OnDisable()
	{
		_ = base.gameObject.activeInHierarchy;
	}

	private void OnDestroy()
	{
	}
}
