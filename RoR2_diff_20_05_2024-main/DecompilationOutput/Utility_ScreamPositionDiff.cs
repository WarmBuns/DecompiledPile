using UnityEngine;
using UnityEngine.Networking;

public class Utility_ScreamPositionDiff : MonoBehaviour
{
	private Vector3 _prevPosition;

	private void OnEnable()
	{
		_prevPosition = base.transform.position;
	}

	private void FixedUpdate()
	{
		Vector3 position = base.transform.position;
		if (!NetworkServer.active && _prevPosition != position)
		{
			_prevPosition = position;
		}
	}
}
