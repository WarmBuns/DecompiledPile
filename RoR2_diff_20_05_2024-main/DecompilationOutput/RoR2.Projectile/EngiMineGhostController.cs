using EntityStates.Engi.Mine;
using UnityEngine;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileGhostController))]
public class EngiMineGhostController : MonoBehaviour
{
	private ProjectileGhostController projectileGhostController;

	private EntityStateMachine armingStateMachine;

	[Tooltip("Child object which will be enabled if the projectile is armed.")]
	public GameObject stickIndicator;

	private bool cachedArmed;

	private void Awake()
	{
		projectileGhostController = GetComponent<ProjectileGhostController>();
		stickIndicator.SetActive(value: false);
	}

	private void Start()
	{
		Transform authorityTransform = projectileGhostController.authorityTransform;
		if ((bool)authorityTransform)
		{
			armingStateMachine = EntityStateMachine.FindByCustomName(authorityTransform.gameObject, "Arming");
		}
	}

	private bool IsArmed()
	{
		return ((armingStateMachine?.state as BaseMineArmingState)?.damageScale ?? 0f) > 1f;
	}

	private void FixedUpdate()
	{
		bool flag = IsArmed();
		if (flag != cachedArmed)
		{
			cachedArmed = flag;
			stickIndicator.SetActive(flag);
		}
	}
}
