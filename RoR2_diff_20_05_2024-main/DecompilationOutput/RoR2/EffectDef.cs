using System;
using UnityEngine;

namespace RoR2;

public class EffectDef
{
	private GameObject _prefab;

	public Func<EffectData, bool> cullMethod;

	public EffectIndex index { get; internal set; } = EffectIndex.Invalid;

	public GameObject prefab
	{
		get
		{
			return _prefab;
		}
		set
		{
			if (!value)
			{
				throw new ArgumentNullException("Prefab is invalid.");
			}
			if ((object)_prefab != value)
			{
				EffectComponent component = value.GetComponent<EffectComponent>();
				if (!component)
				{
					throw new ArgumentException($"Prefab \"{value}\" does not have EffectComponent attached.");
				}
				_prefab = value;
				prefabEffectComponent = component;
				prefabVfxAttributes = _prefab.GetComponent<VFXAttributes>();
				prefabName = _prefab.name;
				spawnSoundEventName = prefabEffectComponent.soundName;
			}
		}
	}

	public EffectComponent prefabEffectComponent { get; private set; }

	public VFXAttributes prefabVfxAttributes { get; private set; }

	public string prefabName { get; private set; }

	public string spawnSoundEventName { get; private set; }

	public EffectDef()
	{
	}

	public EffectDef(GameObject effectPrefab)
	{
		prefab = effectPrefab;
	}
}
