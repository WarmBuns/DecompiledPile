using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.Moon;

public class MoonBatteryBloodActive : MoonBatteryActive
{
	[SerializeField]
	public GameObject siphonPrefab;

	[SerializeField]
	public string siphonRootName;

	private GameObject siphonObject;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			Transform transform = FindModelChild(siphonRootName);
			if (!transform)
			{
				transform = base.transform;
			}
			siphonObject = Object.Instantiate(siphonPrefab, transform.position, transform.rotation, transform);
			NetworkServer.Spawn(siphonObject);
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active && (bool)siphonObject)
		{
			NetworkServer.Destroy(siphonObject);
		}
		base.OnExit();
	}
}
