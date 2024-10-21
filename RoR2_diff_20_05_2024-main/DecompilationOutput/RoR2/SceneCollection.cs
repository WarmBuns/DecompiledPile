using System;
using System.Collections.Generic;
using HG;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/SceneCollection")]
public class SceneCollection : ScriptableObject
{
	[Serializable]
	public struct SceneEntry
	{
		public SceneDef sceneDef;

		[SerializeField]
		private float weightMinusOne;

		public float weight
		{
			get
			{
				return weightMinusOne + 1f;
			}
			set
			{
				weightMinusOne = value - 1f;
			}
		}
	}

	[FormerlySerializedAs("sceneReferences")]
	[SerializeField]
	private SceneEntry[] _sceneEntries = Array.Empty<SceneEntry>();

	public ReadOnlyArray<SceneEntry> sceneEntries => _sceneEntries;

	public bool isEmpty => _sceneEntries.Length == 0;

	public void SetSceneEntries(IReadOnlyList<SceneEntry> newSceneReferences)
	{
		Array.Resize(ref _sceneEntries, newSceneReferences.Count);
		for (int i = 0; i < 0; i++)
		{
			_sceneEntries[i] = newSceneReferences[i];
		}
	}

	public void AddToWeightedSelection(WeightedSelection<SceneDef> dest, Func<SceneDef, bool> canAdd = null)
	{
		foreach (SceneEntry sceneEntry in sceneEntries)
		{
			SceneDef sceneDef = sceneEntry.sceneDef;
			if ((bool)sceneDef && (canAdd == null || canAdd(sceneDef)))
			{
				dest.AddChoice(sceneDef, sceneEntry.weight);
			}
		}
	}
}
