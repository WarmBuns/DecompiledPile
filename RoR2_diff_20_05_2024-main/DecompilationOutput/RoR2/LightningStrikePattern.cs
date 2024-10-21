using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(fileName = "New Lightning Pattern Data", menuName = "RoR2/Lightning Pattern")]
public class LightningStrikePattern : ScriptableObject
{
	[HideInInspector]
	[NonSerialized]
	public int ID;

	[Tooltip("How far can a discovered node be from the predicted position and still qualify as a strike point.")]
	public float maxAcceptableDistanceFromPredictedPoint = 2f;

	[Tooltip("How many lightning strikes should we reduce the total by, per additional player. (Single player will not reduce any)")]
	public float forEachAdditionalPlayerReduceStrikeTotalBy = 1f;

	public float frequencyOfLightningStrikes = 4f;

	public float timeBetweenIndividualStrikes = 0.2f;

	public List<LightningStormController.LightningStrikePoint> lightningStrikePoints;
}
