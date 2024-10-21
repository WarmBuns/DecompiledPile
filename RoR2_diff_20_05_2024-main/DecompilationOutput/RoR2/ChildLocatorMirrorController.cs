using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class ChildLocatorMirrorController : MonoBehaviour
{
	private class MirrorPair
	{
		public Transform referenceTransform;

		public Transform targetTransform;
	}

	[SerializeField]
	[Tooltip("The ChildLocator we are using are a reference to GET the transform information")]
	private ChildLocator _referenceLocator;

	[SerializeField]
	[Tooltip("The ChildLocator we are targeting to SET the transform information")]
	private ChildLocator _targetLocator;

	private List<MirrorPair> mirrorPairs = new List<MirrorPair>();

	public ChildLocator referenceLocator
	{
		get
		{
			return _referenceLocator;
		}
		set
		{
			_referenceLocator = value;
			mirrorPairs.Clear();
			if ((bool)_referenceLocator && (bool)_targetLocator)
			{
				RebuildMirrorPairs();
			}
		}
	}

	public ChildLocator targetLocator
	{
		get
		{
			return _targetLocator;
		}
		set
		{
			_targetLocator = value;
			mirrorPairs.Clear();
			if ((bool)_referenceLocator && (bool)_targetLocator)
			{
				RebuildMirrorPairs();
			}
		}
	}

	private void Start()
	{
		if ((bool)_referenceLocator && (bool)_targetLocator)
		{
			RebuildMirrorPairs();
		}
	}

	private void FixedUpdate()
	{
		foreach (MirrorPair mirrorPair in mirrorPairs)
		{
			if ((bool)mirrorPair.referenceTransform && (bool)mirrorPair.targetTransform)
			{
				mirrorPair.targetTransform.position = mirrorPair.referenceTransform.position;
				mirrorPair.targetTransform.rotation = mirrorPair.referenceTransform.rotation;
			}
		}
	}

	private void RebuildMirrorPairs()
	{
		mirrorPairs.Clear();
		for (int i = 0; i < _targetLocator.Count; i++)
		{
			string childName = _targetLocator.FindChildName(i);
			Transform transform = _targetLocator.FindChild(i);
			Transform transform2 = _referenceLocator.FindChild(childName);
			if ((bool)transform && (bool)transform2)
			{
				mirrorPairs.Add(new MirrorPair
				{
					targetTransform = transform,
					referenceTransform = transform2
				});
			}
		}
	}
}
