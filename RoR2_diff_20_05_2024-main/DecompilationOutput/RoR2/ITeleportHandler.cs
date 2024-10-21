using UnityEngine;
using UnityEngine.EventSystems;

namespace RoR2;

public interface ITeleportHandler : IEventSystemHandler
{
	void OnTeleport(Vector3 oldPosition, Vector3 newPosition);
}
