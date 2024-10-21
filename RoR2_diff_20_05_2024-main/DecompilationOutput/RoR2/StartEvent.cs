using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class StartEvent : MonoBehaviour
{
	public bool runOnServerOnly;

	public UnityEvent action;

	private void Start()
	{
		if (!runOnServerOnly || NetworkServer.active)
		{
			action.Invoke();
		}
	}
}
