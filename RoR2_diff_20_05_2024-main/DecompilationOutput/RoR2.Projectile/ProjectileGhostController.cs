using UnityEngine;
using UnityEngine.Events;

namespace RoR2.Projectile;

public class ProjectileGhostController : MonoBehaviour
{
	private new Transform transform;

	[HideInInspector]
	public EffectManagerHelper emh;

	private float migration = 1f;

	[Tooltip("Sets the ghost's scale to match the base projectile.")]
	public bool inheritScaleFromProjectile;

	[Tooltip("When my local projectile 'parent' is destroyed, run these.")]
	public UnityEvent ParentProjectileDestroyedEvent;

	private bool alreadyRunParentProjectileDestroyedEvent;

	public Transform authorityTransform { get; set; }

	public Transform predictionTransform { get; set; }

	private void Awake()
	{
		transform = base.transform;
	}

	private void Start()
	{
		if (TryGetComponent<EffectManagerHelper>(out var component))
		{
			emh = component;
		}
	}

	private void OnEnable()
	{
		alreadyRunParentProjectileDestroyedEvent = false;
	}

	private void FixedUpdate()
	{
		if ((bool)authorityTransform ^ (bool)predictionTransform)
		{
			CopyTransform(authorityTransform ? authorityTransform : predictionTransform);
		}
		else if ((bool)authorityTransform)
		{
			LerpTransform(predictionTransform, authorityTransform, migration);
			if (migration == 1f)
			{
				predictionTransform = null;
			}
		}
		else if ((bool)emh && emh.OwningPool != null)
		{
			if (!emh.DontAllowProjectilesToPrematurelyPool)
			{
				emh.OwningPool.ReturnObject(emh);
			}
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void LerpTransform(Transform a, Transform b, float t)
	{
		transform.position = Vector3.LerpUnclamped(a.position, b.position, t);
		transform.rotation = Quaternion.SlerpUnclamped(a.rotation, b.rotation, t);
		if (inheritScaleFromProjectile)
		{
			transform.localScale = Vector3.Lerp(a.localScale, b.localScale, t);
		}
	}

	private void CopyTransform(Transform src)
	{
		transform.position = src.position;
		transform.rotation = src.rotation;
		if (inheritScaleFromProjectile)
		{
			transform.localScale = src.localScale;
		}
	}

	public void OnParentProjectileDestroyed()
	{
		if (!alreadyRunParentProjectileDestroyedEvent)
		{
			ParentProjectileDestroyedEvent.Invoke();
			alreadyRunParentProjectileDestroyedEvent = true;
		}
	}
}
