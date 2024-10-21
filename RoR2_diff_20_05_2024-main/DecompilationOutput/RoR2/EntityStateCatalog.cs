using System;
using System.Collections;
using System.Collections.Generic;
using EntityStates;
using HG;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using UnityEngine;

namespace RoR2;

public static class EntityStateCatalog
{
	private static readonly Dictionary<Type, EntityStateIndex> stateTypeToIndex = new Dictionary<Type, EntityStateIndex>(1000);

	private static Type[] stateIndexToType = Array.Empty<Type>();

	private static readonly Dictionary<Type, Action<object>> instanceFieldInitializers = new Dictionary<Type, Action<object>>();

	private static string[] stateIndexToTypeName = Array.Empty<string>();

	[Obsolete("This is only for use in legacy editors only until they're fully phased out.")]
	public static string[] baseGameStateTypeNames = new string[0];

	private static void SetStateInstanceInitializer([NotNull] Type stateType, [NotNull] Action<object> initializer)
	{
		if (typeof(EntityState).IsAssignableFrom(stateType))
		{
			instanceFieldInitializers[stateType] = initializer;
		}
	}

	public static void InitializeStateFields([NotNull] EntityState entityState)
	{
		if (instanceFieldInitializers.TryGetValue(entityState.GetType(), out var value))
		{
			value(entityState);
		}
	}

	private static IEnumerator SetElements(Type[] newEntityStateTypes, EntityStateConfiguration[] newEntityStateConfigurations)
	{
		ArrayUtils.CloneTo(newEntityStateTypes, ref stateIndexToType);
		int _length = stateIndexToType.Length;
		string[] typeNames = new string[_length];
		for (int i = 0; i < _length; i++)
		{
			typeNames[i] = stateIndexToType[i].AssemblyQualifiedName;
		}
		yield return null;
		Array.Sort(typeNames, stateIndexToType, StringComparer.Ordinal);
		yield return null;
		stateTypeToIndex.Clear();
		for (int j = 0; j < _length; j++)
		{
			Type key = stateIndexToType[j];
			stateTypeToIndex[key] = (EntityStateIndex)j;
		}
		yield return null;
		Array.Resize(ref stateIndexToTypeName, stateIndexToType.Length);
		for (int k = 0; k < stateIndexToType.Length; k++)
		{
			stateIndexToTypeName[k] = stateIndexToType[k].Name;
		}
		yield return null;
		instanceFieldInitializers.Clear();
		int limitForLoop = 10;
		int loopIndex = 0;
		for (_length = 0; _length < newEntityStateConfigurations.Length; _length++)
		{
			ApplyEntityStateConfiguration(newEntityStateConfigurations[_length]);
			if (loopIndex++ >= limitForLoop)
			{
				loopIndex = 0;
				yield return null;
			}
		}
	}

	private static void ApplyEntityStateConfiguration(EntityStateConfiguration entityStateConfiguration)
	{
		Type type = (Type)entityStateConfiguration.targetType;
		if (type == null)
		{
			Debug.LogFormat("ApplyEntityStateConfiguration({0}) failed: state type is null.", entityStateConfiguration);
			return;
		}
		if (!stateTypeToIndex.ContainsKey(type))
		{
			Debug.LogFormat("ApplyEntityStateConfiguration({0}) failed: state type {1} is not registered.", entityStateConfiguration, type.FullName);
			return;
		}
		entityStateConfiguration.ApplyStatic();
		Action<object> action = entityStateConfiguration.BuildInstanceInitializer();
		if (action == null)
		{
			instanceFieldInitializers.Remove(type);
		}
		else
		{
			instanceFieldInitializers[type] = action;
		}
	}

	private static void OnStateConfigurationUpdatedByEditor(EntityStateConfiguration entityStateConfiguration)
	{
		ApplyEntityStateConfiguration(entityStateConfiguration);
	}

	public static IEnumerator Init()
	{
		EntityStateConfiguration.onEditorUpdatedConfigurationGlobal += OnStateConfigurationUpdatedByEditor;
		yield return SetElements(ContentManager.entityStateTypes, ContentManager.entityStateConfigurations);
	}

	public static EntityStateIndex GetStateIndex(Type entityStateType)
	{
		if (stateTypeToIndex.TryGetValue(entityStateType, out var value))
		{
			return value;
		}
		return EntityStateIndex.Invalid;
	}

	public static Type GetStateType(EntityStateIndex entityStateIndex)
	{
		return ArrayUtils.GetSafe(stateIndexToType, (int)entityStateIndex, (Type)null);
	}

	[CanBeNull]
	public static string GetStateTypeName(Type entityStateType)
	{
		return GetStateTypeName(GetStateIndex(entityStateType));
	}

	[CanBeNull]
	public static string GetStateTypeName(EntityStateIndex entityStateIndex)
	{
		return ArrayUtils.GetSafe(stateIndexToTypeName, (int)entityStateIndex);
	}

	public static EntityState InstantiateState(EntityStateIndex entityStateIndex)
	{
		Type stateType = GetStateType(entityStateIndex);
		if (stateType != null)
		{
			return Activator.CreateInstance(stateType) as EntityState;
		}
		Debug.LogFormat("Bad stateTypeIndex {0}", entityStateIndex);
		return null;
	}

	public static EntityState InstantiateState(Type stateType)
	{
		if (stateType != null && stateType.IsSubclassOf(typeof(EntityState)))
		{
			return Activator.CreateInstance(stateType) as EntityState;
		}
		Debug.LogFormat("Bad stateType {0}", (stateType == null) ? "null" : stateType.FullName);
		return null;
	}

	public static EntityState InstantiateState(ref SerializableEntityStateType serializableStateType)
	{
		return InstantiateState(serializableStateType.stateType);
	}
}
