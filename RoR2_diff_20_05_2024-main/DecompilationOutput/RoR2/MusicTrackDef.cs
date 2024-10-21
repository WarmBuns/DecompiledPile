using System;
using AK.Wwise;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/MusicTrackDef")]
public class MusicTrackDef : ScriptableObject
{
	public Bank soundBank;

	public State[] states;

	[Multiline]
	public string comment;

	private string _cachedName;

	public MusicTrackIndex catalogIndex { get; set; }

	[Obsolete(".name should not be used. Use .cachedName instead. If retrieving the value from the engine is absolutely necessary, cast to ScriptableObject first.", true)]
	public new string name
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public string cachedName
	{
		get
		{
			return _cachedName;
		}
		set
		{
			base.name = value;
			_cachedName = value;
		}
	}

	private void Awake()
	{
		_cachedName = base.name;
	}

	private void OnValidate()
	{
		_cachedName = base.name;
	}

	public virtual void Preload()
	{
		soundBank?.Load();
	}

	public virtual void Play()
	{
		Preload();
		State[] array = states;
		foreach (State state in array)
		{
			AkSoundEngine.SetState(state.GroupId, state.Id);
		}
	}

	public virtual void Stop()
	{
		if (states != null)
		{
			State[] array = states;
			for (int i = 0; i < array.Length; i++)
			{
				AkSoundEngine.SetState(array[i].GroupId, 0u);
			}
		}
	}
}
