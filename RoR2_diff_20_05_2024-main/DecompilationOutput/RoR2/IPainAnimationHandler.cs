using UnityEngine;

namespace RoR2;

public interface IPainAnimationHandler
{
	void HandlePain(float damage, Vector3 position);
}
