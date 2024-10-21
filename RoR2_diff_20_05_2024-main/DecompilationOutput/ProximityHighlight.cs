using System;
using RoR2;
using UnityEngine;

public class ProximityHighlight : MonoBehaviour
{
	public float highlightRadius = 50f;

	public float highlightScale = 0.6f;

	public float highlightUpdateInterval = 0.1f;

	private float _highlightUpdateTimer;

	private CharacterMaster _characterMaster;

	private CharacterBody _characterBody;

	private InteractionDriver _interactionDriver;

	private Collider[] _hitColliders = new Collider[64];

	private int _hits;

	private void Awake()
	{
		_characterMaster = GetComponent<CharacterMaster>();
		base.enabled = TutorialManager.isTutorialEnabled;
	}

	private void OnEnable()
	{
		OutlineHighlight.onPreRenderOutlineHighlight = (Action<OutlineHighlight>)Delegate.Combine(OutlineHighlight.onPreRenderOutlineHighlight, new Action<OutlineHighlight>(OnPreRenderOutlineHighlight));
	}

	private void OnDisable()
	{
		OutlineHighlight.onPreRenderOutlineHighlight = (Action<OutlineHighlight>)Delegate.Remove(OutlineHighlight.onPreRenderOutlineHighlight, new Action<OutlineHighlight>(OnPreRenderOutlineHighlight));
	}

	private void FixedUpdate()
	{
		if (!_characterMaster)
		{
			return;
		}
		_highlightUpdateTimer -= Time.fixedDeltaTime;
		if (!(_highlightUpdateTimer < 0f))
		{
			return;
		}
		_highlightUpdateTimer = highlightUpdateInterval;
		_characterBody = _characterMaster.GetBody();
		if ((bool)_characterBody)
		{
			_interactionDriver = _characterBody.GetComponent<InteractionDriver>();
			if ((bool)_interactionDriver)
			{
				Array.Clear(_hitColliders, 0, _hitColliders.Length);
				_hits = Physics.OverlapSphereNonAlloc(_characterBody.transform.position, highlightRadius, _hitColliders, LayerIndex.CommonMasks.interactable, QueryTriggerInteraction.Collide);
			}
		}
	}

	private void OnPreRenderOutlineHighlight(OutlineHighlight outlineHighlight)
	{
		for (int i = 0; i < _hits; i++)
		{
			if (!_hitColliders[i] || !_hitColliders[i].gameObject || !EntityLocator.HasEntityLocator(_hitColliders[i].gameObject, out var foundLocator))
			{
				continue;
			}
			GameObject entity = foundLocator.entity;
			if (_interactionDriver.currentInteractable == entity)
			{
				continue;
			}
			IInteractable component = entity.GetComponent<IInteractable>();
			if (component != null && ((MonoBehaviour)component).isActiveAndEnabled && (bool)_interactionDriver.interactor && component.GetInteractability(_interactionDriver.interactor) != 0 && component.ShouldProximityHighlight())
			{
				Highlight component2 = entity.GetComponent<Highlight>();
				if (!component2)
				{
					break;
				}
				Color color = component2.GetColor() * component2.strength * highlightScale;
				outlineHighlight.highlightQueue.Enqueue(new OutlineHighlight.HighlightInfo
				{
					renderer = component2.targetRenderer,
					color = color
				});
			}
		}
	}
}
