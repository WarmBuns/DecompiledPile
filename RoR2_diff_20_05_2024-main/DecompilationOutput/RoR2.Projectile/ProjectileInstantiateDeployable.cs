using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class ProjectileInstantiateDeployable : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The deployable slot to use.")]
	private DeployableSlot deployableSlot;

	[SerializeField]
	[Tooltip("The prefab to instantiate.")]
	private GameObject prefab;

	[SerializeField]
	[Tooltip("The object upon which the prefab will be positioned.")]
	private Transform targetTransform;

	[SerializeField]
	[Tooltip("The transform upon which to instantiate the prefab.")]
	private bool copyTargetRotation;

	[SerializeField]
	[Tooltip("Whether or not to parent the instantiated prefab to the specified transform.")]
	private bool parentToTarget;

	[SerializeField]
	[Tooltip("Whether or not to instantiate this prefab. If so, this will only run on the server, and will be spawned over the network.")]
	private bool instantiateOnStart = true;

	public void Start()
	{
		if (instantiateOnStart)
		{
			InstantiateDeployable();
		}
	}

	public void InstantiateDeployable()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		GameObject owner = GetComponent<ProjectileController>().owner;
		if (!owner)
		{
			return;
		}
		CharacterBody component = owner.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			CharacterMaster master = component.master;
			if ((bool)master)
			{
				Vector3 position = (targetTransform ? targetTransform.position : Vector3.zero);
				Quaternion rotation = (copyTargetRotation ? targetTransform.rotation : Quaternion.identity);
				Transform parent = (parentToTarget ? targetTransform : null);
				GameObject gameObject = Object.Instantiate(prefab, position, rotation, parent);
				NetworkServer.Spawn(gameObject);
				master.AddDeployable(gameObject.GetComponent<Deployable>(), deployableSlot);
			}
		}
	}
}
