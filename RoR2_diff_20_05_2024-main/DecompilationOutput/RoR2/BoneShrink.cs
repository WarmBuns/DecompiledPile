using UnityEngine;

namespace RoR2;

public class BoneShrink : MonoBehaviour
{
	private new Transform transform;

	private void Awake()
	{
		transform = base.transform;
	}

	private void LateUpdate()
	{
		transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
	}
}
