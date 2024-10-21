using UnityEngine;

namespace RoR2;

public interface IPhysMotor
{
	float mass { get; }

	Vector3 velocity { get; }

	Vector3 velocityAuthority { get; set; }

	void ApplyForceImpulse(in PhysForceInfo physForceInfo);
}
