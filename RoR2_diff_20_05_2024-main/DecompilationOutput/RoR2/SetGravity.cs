using UnityEngine;

namespace RoR2;

public class SetGravity : MonoBehaviour
{
	public float newGravity = -30f;

	private void OnEnable()
	{
		Physics.gravity = new Vector3(0f, newGravity, 0f);
	}

	private void OnDisable()
	{
		Physics.gravity = new Vector3(0f, Run.baseGravity, 0f);
	}
}
