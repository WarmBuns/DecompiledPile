using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HG;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[DisallowMultipleComponent]
[RequireComponent(typeof(Run))]
public class RunArtifactManager : NetworkBehaviour
{
	public delegate void ArtifactStateChangeDelegate([NotNull] RunArtifactManager runArtifactManager, [NotNull] ArtifactDef artifactDef);

	public struct RunEnabledArtifacts : IEnumerator<ArtifactDef>, IEnumerator, IDisposable
	{
		private ArtifactIndex artifactIndex;

		public ArtifactDef Current => ArtifactCatalog.GetArtifactDef(artifactIndex);

		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			RunArtifactManager instance = RunArtifactManager.instance;
			if ((object)instance == null)
			{
				return false;
			}
			while ((int)(++artifactIndex) < ArtifactCatalog.artifactCount)
			{
				if (instance.IsArtifactEnabled(artifactIndex))
				{
					return true;
				}
			}
			return false;
		}

		public void Reset()
		{
			artifactIndex = ArtifactIndex.None;
		}

		public void Dispose()
		{
		}
	}

	private Run run;

	private static readonly uint enabledArtifactsDirtyBit = 1u;

	private static readonly uint allDirtyBits = enabledArtifactsDirtyBit;

	private bool[] _enabledArtifacts;

	private static readonly FixedSizeArrayPool<bool> enabledArtifactMaskPool = new FixedSizeArrayPool<bool>(0);

	public static readonly GenericStaticEnumerable<ArtifactDef, RunEnabledArtifacts> enabledArtifactsEnumerable = default(GenericStaticEnumerable<ArtifactDef, RunEnabledArtifacts>);

	public static RunArtifactManager instance { get; private set; }

	public static event ArtifactStateChangeDelegate onArtifactEnabledGlobal;

	public static event ArtifactStateChangeDelegate onArtifactDisabledGlobal;

	private void Awake()
	{
		_enabledArtifacts = enabledArtifactMaskPool.Request();
		run = GetComponent<Run>();
		Run.onServerRunSetRuleBookGlobal += OnServerRunSetRuleBook;
	}

	private void OnDestroy()
	{
		int i = 0;
		for (int artifactCount = ArtifactCatalog.artifactCount; i < artifactCount; i++)
		{
			SetArtifactEnabled(ArtifactCatalog.GetArtifactDef((ArtifactIndex)i), newEnabled: false);
		}
		Run.onServerRunSetRuleBookGlobal -= OnServerRunSetRuleBook;
		if (_enabledArtifacts != null)
		{
			enabledArtifactMaskPool.Return(_enabledArtifacts);
			_enabledArtifacts = null;
		}
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = (initialState ? allDirtyBits : base.syncVarDirtyBits);
		bool num2 = (num & enabledArtifactsDirtyBit) != 0;
		writer.WritePackedUInt32(num);
		if (num2)
		{
			writer.WriteBitArray(_enabledArtifacts);
		}
		if (!initialState)
		{
			return num != 0;
		}
		return false;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if ((reader.ReadPackedUInt32() & enabledArtifactsDirtyBit) != 0)
		{
			bool[] array = enabledArtifactMaskPool.Request();
			reader.ReadBitArray(array);
			int i = 0;
			for (int artifactCount = ArtifactCatalog.artifactCount; i < artifactCount; i++)
			{
				SetArtifactEnabled(ArtifactCatalog.GetArtifactDef((ArtifactIndex)i), array[i]);
			}
		}
	}

	private void OnServerRunSetRuleBook([NotNull] Run run, [NotNull] RuleBook newRuleBook)
	{
		bool[] array = enabledArtifactMaskPool.Request();
		int i = 0;
		for (int ruleCount = RuleCatalog.ruleCount; i < ruleCount; i++)
		{
			RuleChoiceDef ruleChoice = newRuleBook.GetRuleChoice(i);
			if (ruleChoice.artifactIndex != ArtifactIndex.None)
			{
				array[(int)ruleChoice.artifactIndex] = true;
			}
		}
		int j = 0;
		for (int artifactCount = ArtifactCatalog.artifactCount; j < artifactCount; j++)
		{
			SetArtifactEnabled(ArtifactCatalog.GetArtifactDef((ArtifactIndex)j), array[j]);
		}
		enabledArtifactMaskPool.Return(array);
	}

	private void SetArtifactEnabled([NotNull] ArtifactDef artifactDef, bool newEnabled)
	{
		ref bool reference = ref _enabledArtifacts[(int)artifactDef.artifactIndex];
		if (reference != newEnabled)
		{
			if (NetworkServer.active)
			{
				SetDirtyBit(enabledArtifactsDirtyBit);
			}
			reference = newEnabled;
			(reference ? RunArtifactManager.onArtifactEnabledGlobal : RunArtifactManager.onArtifactDisabledGlobal)?.Invoke(this, artifactDef);
		}
	}

	[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
	private static void Init()
	{
		enabledArtifactMaskPool.lengthOfArrays = ArtifactCatalog.artifactCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsArtifactEnabled([NotNull] ArtifactDef artifactDef)
	{
		return IsArtifactEnabled(artifactDef.artifactIndex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsArtifactEnabled(ArtifactIndex artifactIndex)
	{
		return ArrayUtils.GetSafe(_enabledArtifacts, (int)artifactIndex, false);
	}

	[Server]
	public void SetArtifactEnabledServer(ArtifactDef artifactDef, bool newEnabled)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.RunArtifactManager::SetArtifactEnabledServer(RoR2.ArtifactDef,System.Boolean)' called on client");
		}
		else
		{
			SetArtifactEnabled(artifactDef, newEnabled);
		}
	}

	private void UNetVersion()
	{
	}

	public override void PreStartClient()
	{
	}
}
