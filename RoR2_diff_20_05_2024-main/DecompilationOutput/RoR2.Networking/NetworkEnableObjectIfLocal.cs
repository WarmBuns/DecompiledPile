using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class NetworkEnableObjectIfLocal : NetworkBehaviour
{
	[Tooltip("The GameObject to enable/disable.")]
	public GameObject target;

	private void Start()
	{
		if ((bool)target)
		{
			target.SetActive(base.hasAuthority);
		}
	}

	public override void OnStartAuthority()
	{
		base.OnStartAuthority();
		if ((bool)target)
		{
			target.SetActive(value: true);
		}
	}

	public override void OnStopAuthority()
	{
		if ((bool)target)
		{
			target.SetActive(value: false);
		}
		base.OnStopAuthority();
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
