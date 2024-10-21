using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2.Orbs;

[RequireComponent(typeof(EffectComponent))]
public class OrbEffect : MonoBehaviour
{
	private Transform targetTransform;

	private float duration;

	private Vector3 startPosition;

	private Vector3 previousPosition;

	private Vector3 lastKnownTargetPosition;

	private float age;

	[HideInInspector]
	public Transform parentObjectTransform;

	[Header("Curve Parameters")]
	public Vector3 startVelocity1;

	public Vector3 startVelocity2;

	public Vector3 endVelocity1;

	public Vector3 endVelocity2;

	private Vector3 startVelocity;

	private Vector3 endVelocity;

	public AnimationCurve movementCurve;

	public BezierCurveLine bezierCurveLine;

	public bool faceMovement = true;

	public bool callArrivalIfTargetIsGone;

	[Header("Start Effect")]
	[Tooltip("An effect prefab to spawn on Start")]
	public GameObject startEffect;

	public float startEffectScale = 1f;

	public bool startEffectCopiesRotation;

	[Header("End Effect")]
	[Tooltip("An effect prefab to spawn on end")]
	public GameObject endEffect;

	public float endEffectScale = 1f;

	public bool endEffectCopiesRotation;

	[Header("Arrival Behavior")]
	public UnityEvent onArrival;

	protected EffectComponent _effectComponent;

	protected EffectManagerHelper _effectManagerHelper;

	protected EffectManagerHelper _parentEffectManagerHelper;

	protected EffectData _effectData;

	protected List<TrailRenderer> _trailRenderers;

	protected bool _arrived;

	protected bool _startCalled;

	private bool orbEffectHasOnArrivalAnimateShaderAlpha;

	public int singletonArrayIndex = -1;

	public void PrepForPoolUsage()
	{
		if (onArrival == null)
		{
			return;
		}
		int num = -1;
		int persistentEventCount = onArrival.GetPersistentEventCount();
		for (int i = 0; i < persistentEventCount; i++)
		{
			UnityEngine.Object persistentTarget = onArrival.GetPersistentTarget(i);
			string persistentMethodName = onArrival.GetPersistentMethodName(i);
			if (persistentMethodName == "DetachChildren")
			{
				num = i;
			}
			else if (persistentMethodName == "set_enabled" && persistentTarget.GetType() == typeof(AnimateShaderAlpha))
			{
				orbEffectHasOnArrivalAnimateShaderAlpha = true;
				AnimateShaderAlpha animateShaderAlpha = persistentTarget as AnimateShaderAlpha;
				if (!animateShaderAlpha.continueExistingAfterTimeMaxIsReached)
				{
					animateShaderAlpha.OnFinished.AddListener(OnAllEffectsOnArrivalCompleted);
				}
				if (animateShaderAlpha.destroyOnEnd)
				{
					animateShaderAlpha.destroyOnEnd = false;
					animateShaderAlpha.disableOnEnd = true;
				}
			}
		}
		if (num != -1)
		{
			onArrival.SetPersistentListenerState(num, UnityEventCallState.Off);
		}
		if (_trailRenderers != null)
		{
			return;
		}
		TrailRenderer[] components = base.gameObject.GetComponents<TrailRenderer>();
		if (components != null && components.Length != 0)
		{
			if (_trailRenderers == null)
			{
				_trailRenderers = new List<TrailRenderer>();
			}
			_trailRenderers.AddRange(components);
		}
		TrailRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<TrailRenderer>(includeInactive: true);
		if (componentsInChildren != null && componentsInChildren.Length != 0)
		{
			if (_trailRenderers == null)
			{
				_trailRenderers = new List<TrailRenderer>();
			}
			_trailRenderers.AddRange(componentsInChildren);
		}
	}

	private void OnAllEffectsOnArrivalCompleted()
	{
		if (_effectManagerHelper != null && _effectManagerHelper.OwningPool != null)
		{
			_effectManagerHelper.OwningPool.ReturnObject(_effectManagerHelper);
			return;
		}
		if (_effectManagerHelper != null)
		{
			Debug.LogErrorFormat("OrbEffect: No owning pool set. {0} ({1})", base.gameObject.name, base.gameObject.GetInstanceID());
		}
		UnityEngine.Object.Destroy(base.gameObject);
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
		targetTransform = null;
		duration = 0f;
		startPosition = Vector3.zero;
		previousPosition = Vector3.zero;
		lastKnownTargetPosition = Vector3.zero;
		age = 0f;
		startVelocity = Vector3.zero;
		endVelocity = Vector3.zero;
		parentObjectTransform = null;
		startPosition = _effectComponent.effectData.origin;
		previousPosition = startPosition;
		GameObject gameObject = _effectComponent.effectData.ResolveHurtBoxReference();
		targetTransform = (gameObject ? gameObject.transform : null);
		duration = _effectComponent.effectData.genericFloat;
		if (duration == 0f)
		{
			Debug.LogFormat("Zero duration for effect \"{0}\" {1}:{2}:{3}.{4}", base.gameObject.name, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
			if (!EffectManager.UsePools)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			if (_effectManagerHelper != null && _effectManagerHelper.OwningPool != null)
			{
				_effectManagerHelper.OwningPool.ReturnObject(_effectManagerHelper);
				return;
			}
			if (_effectManagerHelper != null)
			{
				Debug.LogErrorFormat("OrbEffect: No owning pool set. {0} ({1})", base.gameObject.name, base.gameObject.GetInstanceID());
			}
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		lastKnownTargetPosition = (targetTransform ? targetTransform.position : startPosition);
		if ((bool)startEffect)
		{
			if (_effectData == null)
			{
				_effectData = new EffectData();
			}
			_effectData.Reset();
			_effectData.origin = base.transform.position;
			_effectData.scale = startEffectScale;
			if (startEffectCopiesRotation)
			{
				_effectData.rotation = base.transform.rotation;
			}
			EffectManager.SpawnEffect(startEffect, _effectData, transmit: false);
		}
		startVelocity.x = Mathf.Lerp(startVelocity1.x, startVelocity2.x, UnityEngine.Random.value);
		startVelocity.y = Mathf.Lerp(startVelocity1.y, startVelocity2.y, UnityEngine.Random.value);
		startVelocity.z = Mathf.Lerp(startVelocity1.z, startVelocity2.z, UnityEngine.Random.value);
		endVelocity.x = Mathf.Lerp(endVelocity1.x, endVelocity2.x, UnityEngine.Random.value);
		endVelocity.y = Mathf.Lerp(endVelocity1.y, endVelocity2.y, UnityEngine.Random.value);
		endVelocity.z = Mathf.Lerp(endVelocity1.z, endVelocity2.z, UnityEngine.Random.value);
		if (_trailRenderers != null)
		{
			foreach (TrailRenderer trailRenderer in _trailRenderers)
			{
				trailRenderer.Clear();
			}
		}
		_arrived = false;
		UpdateOrb(0f);
	}

	private void OnEnable()
	{
		OrbEffectSingleton.RegisterInstance(this);
	}

	private void OnDisable()
	{
		OrbEffectSingleton.UnregisterInstance(this);
	}

	private void Start()
	{
		_startCalled = true;
		EffectComponent component = GetComponent<EffectComponent>();
		_effectComponent = component;
		_effectManagerHelper = base.gameObject.GetComponent<EffectManagerHelper>();
		_parentEffectManagerHelper = ((base.gameObject.transform.parent != null) ? base.gameObject.transform.parent.gameObject.GetComponent<EffectManagerHelper>() : null);
		Reset();
	}

	public void UpdateOrb(float deltaTime)
	{
		if (_arrived)
		{
			return;
		}
		if ((bool)parentObjectTransform)
		{
			startPosition = parentObjectTransform.position;
		}
		if ((bool)targetTransform)
		{
			lastKnownTargetPosition = targetTransform.position;
		}
		float num = ((duration == 0f) ? 0f : Mathf.Clamp01(age / duration));
		float num2 = movementCurve.Evaluate(num);
		Vector3 vector = Vector3.Lerp(startPosition + startVelocity * num2, lastKnownTargetPosition + endVelocity * (1f - num2), num2);
		base.transform.position = vector;
		if (faceMovement && vector != previousPosition)
		{
			base.transform.forward = vector - previousPosition;
		}
		UpdateBezier();
		if ((num >= 1f || (callArrivalIfTargetIsGone && targetTransform == null)) && !_arrived)
		{
			onArrival.Invoke();
			if ((bool)endEffect)
			{
				if (_effectData == null)
				{
					_effectData = new EffectData();
				}
				_effectData.Reset();
				_effectData.origin = base.transform.position;
				_effectData.scale = endEffectScale;
				if (endEffectCopiesRotation)
				{
					_effectData.rotation = base.transform.rotation;
				}
				EffectManager.SpawnEffect(endEffect, _effectData, transmit: false);
			}
			if (!EffectManager.UsePools)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
			else if (_effectManagerHelper != null && _effectManagerHelper.OwningPool != null)
			{
				if (!_effectManagerHelper.HasDestroyOnTimer && !_effectManagerHelper.HasAnimateShaderAlphas && !orbEffectHasOnArrivalAnimateShaderAlpha)
				{
					_effectManagerHelper.OwningPool.ReturnObject(_effectManagerHelper);
				}
			}
			else
			{
				if (_effectManagerHelper != null)
				{
					Debug.LogErrorFormat("OrbEffect: No owning pool set. {0} ({1})", base.gameObject.name, base.gameObject.GetInstanceID());
				}
				UnityEngine.Object.Destroy(base.gameObject);
			}
			_arrived = true;
		}
		if (!_arrived)
		{
			previousPosition = vector;
			age += deltaTime;
		}
	}

	private void UpdateBezier()
	{
		if ((bool)bezierCurveLine)
		{
			bezierCurveLine.p1 = startPosition;
			bezierCurveLine.v0 = endVelocity;
			bezierCurveLine.v1 = startVelocity;
			bezierCurveLine.UpdateBezier(0f);
		}
	}

	public void InstantiatePrefab(GameObject prefab)
	{
		UnityEngine.Object.Instantiate(prefab, base.transform.position, base.transform.rotation);
	}

	public void InstantiateEffect(GameObject prefab)
	{
		EffectManager.SpawnEffect(prefab, new EffectData
		{
			origin = base.transform.position
		}, transmit: false);
	}

	public void InstantiateEffectCopyRotation(GameObject prefab)
	{
		EffectManager.SpawnEffect(prefab, new EffectData
		{
			origin = base.transform.position,
			rotation = base.transform.rotation
		}, transmit: false);
	}

	public void InstantiateEffectOppositeFacing(GameObject prefab)
	{
		EffectManager.SpawnEffect(prefab, new EffectData
		{
			origin = base.transform.position,
			rotation = Util.QuaternionSafeLookRotation(-base.transform.forward)
		}, transmit: false);
	}

	public void InstantiatePrefabOppositeFacing(GameObject prefab)
	{
		UnityEngine.Object.Instantiate(prefab, base.transform.position, Util.QuaternionSafeLookRotation(-base.transform.forward));
	}
}
