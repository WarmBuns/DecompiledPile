using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class RunEventFlagResponse : MonoBehaviour
{
	public string flagName;

	public UnityEvent onAwakeWithFlagSetServer;

	public UnityEvent onAwakeWithFlagUnsetServer;

	private void Awake()
	{
		if (NetworkServer.active)
		{
			if ((bool)Run.instance)
			{
				(Run.instance.GetEventFlag(flagName) ? onAwakeWithFlagSetServer : onAwakeWithFlagUnsetServer)?.Invoke();
				return;
			}
			Debug.LogErrorFormat("Cannot handle run event flag response {0}: No run exists.", base.gameObject.name);
		}
	}
}
