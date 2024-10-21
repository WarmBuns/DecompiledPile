using UnityEngine;

namespace RoR2;

public interface IZone
{
	bool IsInBounds(Vector3 position);
}
