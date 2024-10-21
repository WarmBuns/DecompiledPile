using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class OnDisableEvent : MonoBehaviour
{
	public UnityEvent action;

	private void OnDisable()
	{
		action?.Invoke();
	}
}
