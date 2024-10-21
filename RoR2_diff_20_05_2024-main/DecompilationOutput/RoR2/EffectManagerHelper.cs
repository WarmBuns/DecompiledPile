using System;
using System.Collections.Generic;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

public class EffectManagerHelper : MonoBehaviour
{
	protected List<ParticleSystem> _ParticleSystems;

	protected EffectComponent _effectComponent;

	protected VFXAttributes _vfxAttributes;

	protected HoverOverHead _hoverOverHead;

	protected List<Tracer> _tracers;

	protected EffectPool _OwningPool;

	protected bool _IsInitialized;

	protected DestroyOnTimer _destroyOnTimer;

	protected OrbEffect _orbEffect;

	protected Tracer _tracer;

	protected List<AnimateShaderAlpha> _AnimateShaderAlphasDestroyOnEnd;

	protected List<AnimateShaderAlpha> _ActiveAnimateShaderAlphasDestroyOnEnd;

	protected List<AnimateShaderAlpha> _AnimateShaderAlphasDisableOnEnd;

	protected List<AnimateShaderAlpha> _ActiveAnimateShaderAlphasDisableOnEnd;

	protected List<AnimateShaderAlpha> _AnimateShaderAlphas;

	protected List<AnimateShaderAlpha> _ActiveAnimateShaderAlphas;

	protected List<TrailRenderer> _TrailRenderers;

	protected List<LineRenderer> _LineRenderers;

	protected LineRenderer _RootLineRenderer;

	protected List<AkEvent> _AkEvents_Start;

	protected List<AkEvent> _AkEvents_Destroy;

	protected List<DestroyOnParticleEnd> _DestroyOnParticleEnds;

	protected List<DetachParticleOnDestroyAndEndEmission> _DetachParticlesOnDestroyAndEndEmissions;

	protected bool _TriggeredDisable;

	protected List<GameObject> _ActiveGameObjects;

	protected List<ShakeEmitter> _ShakeEmitters;

	protected List<OmniEffect> _OmniEffects;

	[FormerlySerializedAs("SimulateDestroyOnTimer")]
	[SerializeField]
	[Tooltip("Some projectile scripts will prematurely pool ghosts that don't appear to need to exist after the projectile is gone. If your ghost is being prematurely pooled by a projectile script, enable this.")]
	public bool DontAllowProjectilesToPrematurelyPool;

	public EffectComponent effectComponent => _effectComponent;

	public EffectPool OwningPool => _OwningPool;

	public bool HasDestroyOnTimer
	{
		get
		{
			if (!DontAllowProjectilesToPrematurelyPool)
			{
				return _destroyOnTimer != null;
			}
			return true;
		}
	}

	public bool HasAnimateShaderAlphas
	{
		get
		{
			if (_ActiveAnimateShaderAlphasDestroyOnEnd == null || _ActiveAnimateShaderAlphasDestroyOnEnd.Count <= 0)
			{
				if (_ActiveAnimateShaderAlphasDisableOnEnd != null)
				{
					return _ActiveAnimateShaderAlphasDisableOnEnd.Count > 0;
				}
				return false;
			}
			return true;
		}
	}

	public bool HasDetachParticlesOnDestroy
	{
		get
		{
			if (_DetachParticlesOnDestroyAndEndEmissions != null)
			{
				return _DetachParticlesOnDestroyAndEndEmissions.Count > 0;
			}
			return false;
		}
	}

	public bool HasDestroyOnParticleEnd
	{
		get
		{
			if (_DestroyOnParticleEnds != null)
			{
				return _DestroyOnParticleEnds.Count > 0;
			}
			return false;
		}
	}

	private void OnDestroy()
	{
		if (OwningPool != null)
		{
			OwningPool.RemoveObject(this);
			SetOwningPool(null);
		}
	}

	public void SetOwningPool(EffectPool inOwner)
	{
		_OwningPool = inOwner;
	}

	protected void FindAndCacheComponents<T>(ref List<T> refList) where T : Component
	{
		T[] componentsInChildren = base.gameObject.GetComponentsInChildren<T>(includeInactive: true);
		if (componentsInChildren != null && componentsInChildren.Length != 0)
		{
			if (refList == null)
			{
				refList = new List<T>();
			}
			T[] array = componentsInChildren;
			foreach (T item in array)
			{
				refList.Add(item);
			}
		}
	}

	protected void RecursiveFindActiveObjects(Transform inTransform, Transform inRootTransform, List<GameObject> inWhitelistObjects)
	{
		if (inWhitelistObjects.Contains(inTransform.gameObject))
		{
			return;
		}
		int childCount = inTransform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			RecursiveFindActiveObjects(inTransform.GetChild(i), inRootTransform, inWhitelistObjects);
		}
		if (inTransform != inRootTransform && inTransform.gameObject.activeSelf)
		{
			if (_ActiveGameObjects == null)
			{
				_ActiveGameObjects = new List<GameObject>();
			}
			_ActiveGameObjects.Add(inTransform.gameObject);
		}
	}

	public void Initialize()
	{
		if (_IsInitialized)
		{
			return;
		}
		_effectComponent = base.gameObject.GetComponent<EffectComponent>();
		_vfxAttributes = base.gameObject.GetComponent<VFXAttributes>();
		_hoverOverHead = base.gameObject.GetComponent<HoverOverHead>();
		if ((bool)_vfxAttributes && _OwningPool != null)
		{
			_OwningPool.DoNotCull = _vfxAttributes.DoNotCullPool;
		}
		if (EffectManager.UsePools)
		{
			FindAndCacheComponents(ref _tracers);
			FindAndCacheComponents(ref _ParticleSystems);
			if (_ParticleSystems != null)
			{
				foreach (ParticleSystem particleSystem in _ParticleSystems)
				{
					if (particleSystem.main.stopAction == ParticleSystemStopAction.Destroy)
					{
						ParticleSystem.MainModule main = particleSystem.main;
						main.stopAction = ParticleSystemStopAction.None;
					}
				}
			}
			_orbEffect = GetComponent<OrbEffect>();
			if (_orbEffect != null)
			{
				_orbEffect.PrepForPoolUsage();
			}
			_tracer = GetComponent<Tracer>();
			if (_tracer != null)
			{
				_tracer.PrepForPoolUsage();
			}
			AnimateShaderAlpha[] componentsInChildren = base.gameObject.GetComponentsInChildren<AnimateShaderAlpha>(includeInactive: true);
			if (componentsInChildren != null && componentsInChildren.Length != 0)
			{
				AnimateShaderAlpha[] array = componentsInChildren;
				foreach (AnimateShaderAlpha animateShaderAlpha in array)
				{
					animateShaderAlpha.initialyEnabled = animateShaderAlpha.enabled;
					if (!animateShaderAlpha.continueExistingAfterTimeMaxIsReached)
					{
						animateShaderAlpha.OnFinished.AddListener(OnAnimateShaderAlphaComplete);
					}
					if (animateShaderAlpha.destroyOnEnd)
					{
						if (_AnimateShaderAlphasDestroyOnEnd == null)
						{
							_AnimateShaderAlphasDestroyOnEnd = new List<AnimateShaderAlpha>();
						}
						_AnimateShaderAlphasDestroyOnEnd.Add(animateShaderAlpha);
						if (animateShaderAlpha.enabled)
						{
							if (_ActiveAnimateShaderAlphasDestroyOnEnd == null)
							{
								_ActiveAnimateShaderAlphasDestroyOnEnd = new List<AnimateShaderAlpha>();
							}
							_ActiveAnimateShaderAlphasDestroyOnEnd.Add(animateShaderAlpha);
						}
						continue;
					}
					if (animateShaderAlpha.disableOnEnd)
					{
						if (_AnimateShaderAlphasDisableOnEnd == null)
						{
							_AnimateShaderAlphasDisableOnEnd = new List<AnimateShaderAlpha>();
						}
						_AnimateShaderAlphasDisableOnEnd.Add(animateShaderAlpha);
						if (animateShaderAlpha.enabled)
						{
							if (_ActiveAnimateShaderAlphasDisableOnEnd == null)
							{
								_ActiveAnimateShaderAlphasDisableOnEnd = new List<AnimateShaderAlpha>();
							}
							_ActiveAnimateShaderAlphasDisableOnEnd.Add(animateShaderAlpha);
						}
						continue;
					}
					if (_AnimateShaderAlphas == null)
					{
						_AnimateShaderAlphas = new List<AnimateShaderAlpha>();
					}
					_AnimateShaderAlphas.Add(animateShaderAlpha);
					if (animateShaderAlpha.enabled)
					{
						if (_ActiveAnimateShaderAlphas == null)
						{
							_ActiveAnimateShaderAlphas = new List<AnimateShaderAlpha>();
						}
						_ActiveAnimateShaderAlphas.Add(animateShaderAlpha);
					}
				}
			}
			if (_ActiveAnimateShaderAlphasDestroyOnEnd != null && OwningPool != null)
			{
				foreach (AnimateShaderAlpha item in _ActiveAnimateShaderAlphasDestroyOnEnd)
				{
					item.destroyOnEnd = false;
					item.disableOnEnd = true;
				}
			}
			FindAndCacheComponents(ref _TrailRenderers);
			if (_TrailRenderers != null)
			{
				foreach (TrailRenderer trailRenderer in _TrailRenderers)
				{
					trailRenderer.autodestruct = false;
				}
			}
			FindAndCacheComponents(ref _LineRenderers);
			_RootLineRenderer = base.gameObject.GetComponent<LineRenderer>();
			if (_RootLineRenderer != null && !_RootLineRenderer.enabled)
			{
				_RootLineRenderer = null;
			}
			AkEvent[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<AkEvent>();
			if (componentsInChildren2 != null && componentsInChildren2.Length != 0)
			{
				AkEvent[] array2 = componentsInChildren2;
				foreach (AkEvent akEvent in array2)
				{
					if (akEvent.triggerList.Contains(1281810935))
					{
						if (_AkEvents_Start == null)
						{
							_AkEvents_Start = new List<AkEvent>();
						}
						_AkEvents_Start.Add(akEvent);
					}
					if (akEvent.triggerList.Contains(-358577003))
					{
						if (_AkEvents_Destroy == null)
						{
							_AkEvents_Destroy = new List<AkEvent>();
						}
						_AkEvents_Destroy.Add(akEvent);
					}
				}
			}
			FindAndCacheComponents(ref _DestroyOnParticleEnds);
			if (_DestroyOnParticleEnds != null && _DestroyOnParticleEnds.Count > 0)
			{
				for (int num = _DestroyOnParticleEnds.Count - 1; num >= 0; num--)
				{
					if (_DestroyOnParticleEnds[num] != null && !_DestroyOnParticleEnds[num].enabled)
					{
						_DestroyOnParticleEnds.RemoveAt(num);
					}
				}
			}
			if (base.gameObject.name.Contains("SlashMercEvis"))
			{
				_ = 0;
			}
			FindAndCacheComponents(ref _ShakeEmitters);
			FindAndCacheComponents(ref _OmniEffects);
			if (_OmniEffects != null && _ParticleSystems != null)
			{
				foreach (OmniEffect omniEffect in _OmniEffects)
				{
					OmniEffect.OmniEffectGroup[] omniEffectGroups = omniEffect.omniEffectGroups;
					for (int i = 0; i < omniEffectGroups.Length; i++)
					{
						OmniEffect.OmniEffectElement[] omniEffectElements = omniEffectGroups[i].omniEffectElements;
						foreach (OmniEffect.OmniEffectElement omniEffectElement in omniEffectElements)
						{
							if (omniEffectElement.particleSystem != null && _ParticleSystems.Contains(omniEffectElement.particleSystem))
							{
								_ParticleSystems.Remove(omniEffectElement.particleSystem);
							}
						}
					}
				}
			}
		}
		_IsInitialized = true;
	}

	public void Reset(bool inActivate)
	{
		if (_effectComponent != null)
		{
			_effectComponent.Reset();
		}
		if (_hoverOverHead != null)
		{
			_hoverOverHead.Reset();
		}
		if (_ParticleSystems != null)
		{
			foreach (ParticleSystem particleSystem in _ParticleSystems)
			{
				try
				{
					particleSystem.Stop();
					particleSystem.Clear();
					if (inActivate)
					{
						particleSystem.Play();
					}
				}
				catch (Exception ex)
				{
					Debug.LogFormat("ERROR: GameObject {0}\n{1}", base.gameObject.name, ex.Message);
				}
			}
		}
		if (_TrailRenderers != null)
		{
			foreach (TrailRenderer trailRenderer in _TrailRenderers)
			{
				trailRenderer.Clear();
			}
		}
		if (_LineRenderers != null)
		{
			for (int i = 0; i < _LineRenderers.Count; i++)
			{
				if (!(_LineRenderers[i] != null))
				{
					continue;
				}
				LineRenderer lineRenderer = _LineRenderers[i];
				if (lineRenderer.positionCount > 1)
				{
					Vector3 position = lineRenderer.GetPosition(0);
					for (int j = 1; j < lineRenderer.positionCount; j++)
					{
						lineRenderer.SetPosition(j, position);
					}
				}
			}
		}
		if (_AnimateShaderAlphasDestroyOnEnd != null)
		{
			foreach (AnimateShaderAlpha item in _AnimateShaderAlphasDestroyOnEnd)
			{
				item.Reset();
			}
		}
		if (_ActiveAnimateShaderAlphasDestroyOnEnd != null)
		{
			foreach (AnimateShaderAlpha item2 in _ActiveAnimateShaderAlphasDestroyOnEnd)
			{
				if (!item2.enabled)
				{
					item2.enabled = true;
				}
			}
		}
		if (_AnimateShaderAlphasDisableOnEnd != null)
		{
			foreach (AnimateShaderAlpha item3 in _AnimateShaderAlphasDisableOnEnd)
			{
				item3.Reset();
			}
		}
		if (_ActiveAnimateShaderAlphasDisableOnEnd != null)
		{
			foreach (AnimateShaderAlpha item4 in _ActiveAnimateShaderAlphasDisableOnEnd)
			{
				if (!item4.enabled)
				{
					item4.enabled = true;
				}
			}
		}
		if (_AnimateShaderAlphas != null)
		{
			foreach (AnimateShaderAlpha animateShaderAlpha in _AnimateShaderAlphas)
			{
				animateShaderAlpha.Reset();
			}
		}
		if (_orbEffect != null)
		{
			_orbEffect.Reset();
		}
		if (_tracer != null)
		{
			_tracer.Reset();
		}
		if (_AkEvents_Start != null)
		{
			foreach (AkEvent item5 in _AkEvents_Start)
			{
				item5.HandleEvent(item5.gameObject);
			}
		}
		if (HasDetachParticlesOnDestroy)
		{
			_TriggeredDisable = false;
			if (_RootLineRenderer != null)
			{
				_RootLineRenderer.enabled = true;
			}
		}
	}

	public void ResetPostActivate()
	{
		if (_ShakeEmitters == null)
		{
			return;
		}
		foreach (ShakeEmitter shakeEmitter in _ShakeEmitters)
		{
			shakeEmitter.StartShake();
		}
	}

	public void StopAllParticleSystems()
	{
		if (_ParticleSystems == null)
		{
			return;
		}
		foreach (ParticleSystem particleSystem in _ParticleSystems)
		{
			try
			{
				particleSystem.Stop();
				particleSystem.Clear();
			}
			catch (Exception ex)
			{
				Debug.LogFormat("ERROR: GameObject {0}\n{1}", base.gameObject.name, ex.Message);
			}
		}
	}

	public void OnAnimateShaderAlphaComplete()
	{
		if (OwningPool != null)
		{
			OwningPool.ReturnObject(this);
			return;
		}
		Debug.LogErrorFormat("No Owning Pool on object {0} ({1})", base.gameObject.name, base.gameObject.GetInstanceID());
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OnReturnToPool()
	{
		if (_AkEvents_Destroy != null)
		{
			foreach (AkEvent item in _AkEvents_Destroy)
			{
				if (item.enabled)
				{
					item.HandleEvent(base.gameObject);
				}
			}
		}
		if (_ActiveGameObjects == null)
		{
			return;
		}
		foreach (GameObject activeGameObject in _ActiveGameObjects)
		{
			activeGameObject.SetActive(value: true);
		}
	}

	public void ReturnToPool()
	{
		EffectComplete();
		OwningPool.ReturnObject(this);
	}

	public bool EffectComplete()
	{
		bool result = true;
		if (HasDetachParticlesOnDestroy && !_TriggeredDisable && _RootLineRenderer != null)
		{
			_RootLineRenderer.enabled = false;
		}
		if (!_TriggeredDisable)
		{
			if (_ActiveGameObjects != null)
			{
				foreach (GameObject activeGameObject in _ActiveGameObjects)
				{
					activeGameObject.SetActive(value: false);
				}
			}
			_TriggeredDisable = true;
		}
		return result;
	}
}
