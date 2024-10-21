using UnityEngine;

namespace RoR2;

public class UnparentOnStart : MonoBehaviour
{
	private void Start()
	{
		base.transform.parent = null;
	}
}
