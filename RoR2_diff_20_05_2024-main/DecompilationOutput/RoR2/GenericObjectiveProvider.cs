using System;
using System.Collections.Generic;
using RoR2.UI;
using UnityEngine;

namespace RoR2;

public class GenericObjectiveProvider : MonoBehaviour
{
	private class GenericObjectiveTracker : ObjectivePanelController.ObjectiveTracker
	{
		private string previousToken;

		protected override bool shouldConsiderComplete
		{
			get
			{
				if (retired)
				{
					return ((GenericObjectiveProvider)sourceDescriptor.source).markCompletedOnRetired;
				}
				return false;
			}
		}

		protected override string GenerateString()
		{
			GenericObjectiveProvider genericObjectiveProvider = (GenericObjectiveProvider)sourceDescriptor.source;
			previousToken = genericObjectiveProvider.objectiveToken;
			return Language.GetString(genericObjectiveProvider.objectiveToken);
		}

		protected override bool IsDirty()
		{
			return ((GenericObjectiveProvider)sourceDescriptor.source).objectiveToken != previousToken;
		}
	}

	public string objectiveToken;

	public bool markCompletedOnRetired = true;

	private static readonly Action<CharacterMaster, List<ObjectivePanelController.ObjectiveSourceDescriptor>> collectObjectiveSourcesDelegate = CollectObjectiveSources;

	private void OnEnable()
	{
		if (!InstanceTracker.Any<GenericObjectiveProvider>())
		{
			ObjectivePanelController.collectObjectiveSources += collectObjectiveSourcesDelegate;
		}
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
		if (!InstanceTracker.Any<GenericObjectiveProvider>())
		{
			ObjectivePanelController.collectObjectiveSources -= collectObjectiveSourcesDelegate;
		}
	}

	private static void CollectObjectiveSources(CharacterMaster viewer, List<ObjectivePanelController.ObjectiveSourceDescriptor> dest)
	{
		foreach (GenericObjectiveProvider instances in InstanceTracker.GetInstancesList<GenericObjectiveProvider>())
		{
			dest.Add(new ObjectivePanelController.ObjectiveSourceDescriptor
			{
				master = viewer,
				objectiveType = typeof(GenericObjectiveTracker),
				source = instances
			});
		}
	}
}
