using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(ProjectileController))]
public class ProjectileCharacterController : MonoBehaviour
{
	private Vector3 downVector;

	public float velocity;

	public float lifetime = 5f;

	private float timer;

	private ProjectileController projectileController;

	private CharacterController characterController;

	private void Awake()
	{
		downVector = Vector3.down * 3f;
		projectileController = GetComponent<ProjectileController>();
		characterController = GetComponent<CharacterController>();
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active || projectileController.isPrediction)
		{
			characterController.Move((base.transform.forward + downVector) * (velocity * Time.fixedDeltaTime));
		}
		if (NetworkServer.active)
		{
			timer += Time.fixedDeltaTime;
			if (timer > lifetime)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
