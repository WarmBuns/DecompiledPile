using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

[RequireComponent(typeof(EffectComponent))]
public class Tracer : MonoBehaviour
{
	[Tooltip("A child transform which will be placed at the start of the tracer path upon creation.")]
	public Transform startTransform;

	[Tooltip("Child object to scale to the length of this tracer and burst particles on based on that length. Optional.")]
	public GameObject beamObject;

	[Tooltip("The number of particles to emit per meter of length if using a beam object.")]
	public float beamDensity = 10f;

	[Tooltip("The travel speed of this tracer.")]
	public float speed = 1f;

	[Tooltip("Child transform which will be moved to the head of the tracer.")]
	public Transform headTransform;

	[Tooltip("Child transform which will be moved to the tail of the tracer.")]
	public Transform tailTransform;

	[Tooltip("The maximum distance between head and tail transforms.")]
	public float length = 1f;

	[Tooltip("Reverses the travel direction of the tracer.")]
	public bool reverse;

	[Tooltip("The event that runs when the tail reaches the destination.")]
	public UnityEvent onTailReachedDestination;

	private Vector3 startPos;

	private Vector3 endPos;

	private float distanceTraveled;

	private float totalDistance;

	private Vector3 normal;

	protected EffectComponent _effectComponent;

	protected EffectManagerHelper _effectManagerHelper;

	protected EffectManagerHelper _parentEffectManagerHelper;

	protected EffectData _effectData;

	protected List<LineRenderer> _LineRenderers;

	protected Renderer[] _renderers;

	protected bool _startCalled;

	protected bool orbEffectHasOnArrivalAnimateShaderAlpha;

	public void PrepForPoolUsage()
	{
		if (onTailReachedDestination == null)
		{
			return;
		}
		int num = -1;
		int persistentEventCount = onTailReachedDestination.GetPersistentEventCount();
		for (int i = 0; i < persistentEventCount; i++)
		{
			Object persistentTarget = onTailReachedDestination.GetPersistentTarget(i);
			switch (onTailReachedDestination.GetPersistentMethodName(i))
			{
			case "UnparentTransform":
				num = i;
				Debug.LogFormat("Disabled DetachChildren");
				onTailReachedDestination.SetPersistentListenerState(num, UnityEventCallState.Off);
				break;
			case "set_enabled":
				if (persistentTarget.GetType() == typeof(AnimateShaderAlpha))
				{
					orbEffectHasOnArrivalAnimateShaderAlpha = true;
					AnimateShaderAlpha animateShaderAlpha = persistentTarget as AnimateShaderAlpha;
					if (animateShaderAlpha.destroyOnEnd)
					{
						animateShaderAlpha.destroyOnEnd = false;
						animateShaderAlpha.disableOnEnd = true;
					}
				}
				break;
			case "DestroySelf":
				Debug.LogFormat("Disabled DestroySelf");
				onTailReachedDestination.SetPersistentListenerState(i, UnityEventCallState.Off);
				break;
			}
		}
		if (_LineRenderers != null)
		{
			return;
		}
		LineRenderer[] components = base.gameObject.GetComponents<LineRenderer>();
		if (components != null && components.Length != 0)
		{
			if (_LineRenderers == null)
			{
				_LineRenderers = new List<LineRenderer>();
			}
			_LineRenderers.AddRange(components);
		}
		LineRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<LineRenderer>(includeInactive: true);
		if (componentsInChildren != null && componentsInChildren.Length != 0)
		{
			if (_LineRenderers == null)
			{
				_LineRenderers = new List<LineRenderer>();
			}
			_LineRenderers.AddRange(componentsInChildren);
		}
	}

	public void Reset()
	{
		if (!_startCalled)
		{
			return;
		}
		if (_effectManagerHelper == null)
		{
			_effectManagerHelper = base.gameObject.GetComponent<EffectManagerHelper>();
		}
		if (_effectComponent == null)
		{
			_effectComponent = base.gameObject.GetComponent<EffectComponent>();
		}
		if (_parentEffectManagerHelper == null)
		{
			_parentEffectManagerHelper = ((base.gameObject.transform.parent != null) ? base.gameObject.transform.parent.gameObject.GetComponent<EffectManagerHelper>() : null);
		}
		if (_effectData != null)
		{
			_effectData.Reset();
		}
		if (_renderers == null)
		{
			_renderers = GetComponents<Renderer>();
		}
		endPos = _effectComponent.effectData.origin;
		Transform transform = _effectComponent.effectData.ResolveChildLocatorTransformReference();
		startPos = (transform ? transform.position : _effectComponent.effectData.start);
		if (reverse)
		{
			Util.Swap(ref endPos, ref startPos);
		}
		Vector3 vector = endPos - startPos;
		distanceTraveled = 0f;
		totalDistance = Vector3.Magnitude(vector);
		if (totalDistance != 0f)
		{
			normal = vector * (1f / totalDistance);
			base.transform.rotation = Util.QuaternionSafeLookRotation(normal);
		}
		else
		{
			normal = Vector3.zero;
		}
		if ((bool)beamObject)
		{
			beamObject.transform.position = startPos + vector * 0.5f;
			ParticleSystem component = beamObject.GetComponent<ParticleSystem>();
			if ((bool)component)
			{
				ParticleSystem.ShapeModule shape = component.shape;
				shape.radius = totalDistance * 0.5f;
				component.Emit(Mathf.FloorToInt(totalDistance * beamDensity) - 1);
			}
		}
		if ((bool)startTransform)
		{
			startTransform.position = startPos;
		}
		SetRenderers(state: true);
	}

	private void Start()
	{
		_startCalled = true;
		Reset();
	}

	private void Update()
	{
		if (distanceTraveled > totalDistance)
		{
			onTailReachedDestination.Invoke();
			Arrive();
			return;
		}
		distanceTraveled += speed * Time.deltaTime;
		float num = Mathf.Clamp(distanceTraveled, 0f, totalDistance);
		float num2 = Mathf.Clamp(distanceTraveled - length, 0f, totalDistance);
		if ((bool)headTransform)
		{
			headTransform.position = startPos + num * normal;
		}
		if ((bool)tailTransform)
		{
			tailTransform.position = startPos + num2 * normal;
		}
	}

	private void SetRenderers(bool state)
	{
		if (_renderers != null)
		{
			for (int i = 0; i < _renderers.Length; i++)
			{
				_renderers[i].enabled = state;
			}
		}
	}

	private void Arrive()
	{
		if (!EffectManager.UsePools)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		SetRenderers(state: false);
		if (_effectManagerHelper != null && _effectManagerHelper.OwningPool != null)
		{
			if (!_effectManagerHelper.HasDestroyOnTimer && !_effectManagerHelper.HasAnimateShaderAlphas && !orbEffectHasOnArrivalAnimateShaderAlpha)
			{
				_effectManagerHelper.OwningPool.ReturnObject(_effectManagerHelper);
			}
			return;
		}
		if (_effectManagerHelper != null)
		{
			Debug.LogErrorFormat("Tracer: No owning pool set. {0} ({1})", base.gameObject.name, base.gameObject.GetInstanceID());
		}
		Object.Destroy(base.gameObject);
	}
}
