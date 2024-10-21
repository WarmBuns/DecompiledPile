using UnityEngine;

namespace RoR2;

public struct PhysForceInfo
{
	[SerializeField]
	public Vector3 force;

	[SerializeField]
	private int flags;

	private static readonly int ignoreGroundStickFlag = 1;

	private static readonly int disableAirControlUntilCollisionFlag = 2;

	private static readonly int massIsOneFlag = 4;

	public bool ignoreGroundStick
	{
		get
		{
			return (flags & ignoreGroundStickFlag) != 0;
		}
		set
		{
			SetFlag(ref flags, ignoreGroundStickFlag, value);
		}
	}

	public bool disableAirControlUntilCollision
	{
		get
		{
			return (flags & disableAirControlUntilCollisionFlag) != 0;
		}
		set
		{
			SetFlag(ref flags, disableAirControlUntilCollisionFlag, value);
		}
	}

	public bool massIsOne
	{
		get
		{
			return (flags & massIsOneFlag) != 0;
		}
		set
		{
			SetFlag(ref flags, massIsOneFlag, value);
		}
	}

	private static void SetFlag(ref int flags, int flag, bool value)
	{
		flags = (value ? (flags | flag) : (flags & ~flag));
	}

	public static PhysForceInfo Create()
	{
		PhysForceInfo result = default(PhysForceInfo);
		result.force = Vector3.zero;
		result.flags = 0;
		result.ignoreGroundStick = false;
		result.disableAirControlUntilCollision = false;
		result.massIsOne = false;
		return result;
	}
}
