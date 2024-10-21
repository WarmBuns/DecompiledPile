using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class AwakeEvent : MonoBehaviour
{
	public UnityEvent action;

	private void Awake()
	{
		action.Invoke();
	}
}
