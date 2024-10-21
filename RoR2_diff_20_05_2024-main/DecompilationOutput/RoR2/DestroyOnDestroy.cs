using UnityEngine;

namespace RoR2;

public class DestroyOnDestroy : MonoBehaviour
{
	[Tooltip("The GameObject to destroy when this object is destroyed.")]
	public GameObject target;

	private void OnDestroy()
	{
		Object.Destroy(target);
	}
}
