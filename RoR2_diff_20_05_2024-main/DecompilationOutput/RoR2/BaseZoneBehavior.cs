using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public abstract class BaseZoneBehavior : NetworkBehaviour, IZone
{
	public abstract bool IsInBounds(Vector3 position);

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
