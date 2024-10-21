using UnityEngine;

namespace RoR2;

public class FadeToBlackOffset : MonoBehaviour
{
	public float value;

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}
}
