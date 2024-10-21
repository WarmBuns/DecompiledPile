using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class OnPlayerEnterEvent : MonoBehaviour
{
	public bool serverOnly;

	public UnityEvent action;

	private bool calledAction;

	private void OnTriggerEnter(Collider other)
	{
		if ((!serverOnly || NetworkServer.active) && !calledAction)
		{
			CharacterBody component = other.GetComponent<CharacterBody>();
			if ((bool)component && component.isPlayerControlled)
			{
				Debug.LogFormat("OnPlayerEnterEvent called on {0}", base.gameObject.name);
				calledAction = true;
				action?.Invoke();
			}
		}
	}
}
