using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class Deployable : MonoBehaviour
{
	[NonSerialized]
	public CharacterMaster ownerMaster;

	public UnityEvent onUndeploy;

	private void OnDestroy()
	{
		if (NetworkServer.active && (bool)ownerMaster)
		{
			ownerMaster.RemoveDeployable(this);
		}
	}

	public void DestroyGameObject()
	{
		if (NetworkServer.active)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}
}
