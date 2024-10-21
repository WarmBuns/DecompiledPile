using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class BoxZone : BaseZoneBehavior, IZone
{
	[SerializeField]
	[Tooltip("If false, \"IsInBounds\" returns true when inside the box.  If true, outside the box is considered in bounds.")]
	private bool isInverted;

	public override bool IsInBounds(Vector3 position)
	{
		Vector3 vector = position - base.transform.position;
		vector.x = Mathf.Abs(vector.x);
		vector.y = Mathf.Abs(vector.y);
		vector.z = Mathf.Abs(vector.z);
		Vector3 vector2 = base.transform.lossyScale * 0.5f;
		if (isInverted)
		{
			if (vector.x > vector2.x && vector.y > vector2.y)
			{
				return vector.z > vector2.z;
			}
			return false;
		}
		if (vector.x < vector2.x && vector.y < vector2.y)
		{
			return vector.z < vector2.z;
		}
		return false;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool flag = base.OnSerialize(writer, forceAll);
		bool flag2 = default(bool);
		return flag2 || flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
	}

	public override void PreStartClient()
	{
		base.PreStartClient();
	}
}
