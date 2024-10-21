using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileDamage))]
public class ProjectileMageFirewallWalkerController : MonoBehaviour
{
	public int totalProjectiles;

	private int _projectileCounter;

	public float dropInterval = 0.15f;

	public GameObject firePillarPrefab;

	public float pillarAngle = 45f;

	public bool curveToCenter = true;

	private float moveSign;

	[SerializeField]
	[Tooltip("Some consoles/etc run a different FixedUpdate timestep. This allows you to run an independent coroutine to get the timing right.")]
	private bool DontUseFixedUpdateForProjectileSpawning;

	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	private Vector3 lastCenterPosition;

	private float timer;

	private Vector3 currentPillarVector = Vector3.up;

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
		lastCenterPosition = base.transform.position;
		timer = dropInterval / 2f;
		moveSign = 1f;
	}

	private void Start()
	{
		if ((bool)projectileController.owner)
		{
			Vector3 position = projectileController.owner.transform.position;
			Vector3 rhs = base.transform.position - position;
			rhs.y = 0f;
			if (rhs.x != 0f && rhs.z != 0f)
			{
				moveSign = Mathf.Sign(Vector3.Dot(base.transform.right, rhs));
			}
		}
		UpdateDirections();
		_projectileCounter = totalProjectiles;
		if (DontUseFixedUpdateForProjectileSpawning)
		{
			StartCoroutine(ProcessProjectilesLoop());
		}
	}

	private void UpdateDirections()
	{
		if (curveToCenter)
		{
			Vector3 vector = base.transform.position - lastCenterPosition;
			vector.y = 0f;
			if (vector.x != 0f && vector.z != 0f)
			{
				vector.Normalize();
				Vector3 vector2 = Vector3.Cross(Vector3.up, vector);
				base.transform.forward = vector2 * moveSign;
				currentPillarVector = Quaternion.AngleAxis(pillarAngle, vector2) * Vector3.Cross(vector, vector2);
			}
		}
	}

	private IEnumerator ProcessProjectilesLoop()
	{
		yield return new WaitForSeconds(dropInterval / 2f);
		while (base.gameObject.activeSelf)
		{
			HandleProjectileGeneration();
			yield return new WaitForSeconds(dropInterval);
		}
	}

	private void FixedUpdate()
	{
		if (!DontUseFixedUpdateForProjectileSpawning)
		{
			HandleProjectileGeneration();
		}
	}

	private void HandleProjectileGeneration()
	{
		if ((bool)projectileController.owner)
		{
			lastCenterPosition = projectileController.owner.transform.position;
		}
		if (_projectileCounter <= 0)
		{
			return;
		}
		UpdateDirections();
		if (!NetworkServer.active)
		{
			return;
		}
		timer -= Time.fixedDeltaTime;
		if (timer <= 0f || DontUseFixedUpdateForProjectileSpawning)
		{
			timer = dropInterval;
			_projectileCounter--;
			if ((bool)firePillarPrefab)
			{
				ProjectileManager.instance.FireProjectile(firePillarPrefab, base.transform.position, Util.QuaternionSafeLookRotation(currentPillarVector), projectileController.owner, projectileDamage.damage, projectileDamage.force, projectileDamage.crit, projectileDamage.damageColorIndex);
			}
		}
	}
}
