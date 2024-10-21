using System;
using System.Collections.Generic;
using System.Reflection;
using EntityStates;
using HG;
using HG.GeneralSerializer;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/EntityStateConfiguration")]
public class EntityStateConfiguration : ScriptableObject
{
	[SerializableSystemType.RequiredBaseType(typeof(EntityState))]
	public SerializableSystemType targetType;

	public SerializedFieldCollection serializedFieldsCollection;

	public static event Action<EntityStateConfiguration> onEditorUpdatedConfigurationGlobal;

	[ContextMenu("Set Name from Target Type")]
	public void SetNameFromTargetType()
	{
		Type type = (Type)targetType;
		if (!(type == null))
		{
			base.name = type.FullName;
		}
	}

	public void ApplyStatic()
	{
		Type type = (Type)targetType;
		if (type == null)
		{
			return;
		}
		SerializedField[] serializedFields = serializedFieldsCollection.serializedFields;
		for (int i = 0; i < serializedFields.Length; i++)
		{
			SerializedField serializedField = serializedFields[i];
			try
			{
				FieldInfo field = type.GetField(serializedField.fieldName, BindingFlags.Static | BindingFlags.Public);
				if ((object)field != null)
				{
					SerializedValue fieldValue = serializedField.fieldValue;
					field.SetValue(null, fieldValue.GetValue(field));
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
	}

	[CanBeNull]
	public Action<object> BuildInstanceInitializer()
	{
		Type type = (Type)targetType;
		if (type == null)
		{
			return null;
		}
		SerializedField[] serializedFields = serializedFieldsCollection.serializedFields;
		List<(FieldInfo, object)> list = CollectionPool<(FieldInfo, object), List<(FieldInfo, object)>>.RentCollection();
		for (int i = 0; i < serializedFields.Length; i++)
		{
			ref SerializedField reference = ref serializedFields[i];
			try
			{
				FieldInfo field = type.GetField(reference.fieldName);
				if (!(field == null) && FieldPassesFilter(field))
				{
					list.Add((field, reference.fieldValue.GetValue(field)));
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		if (list.Count == 0)
		{
			list = CollectionPool<(FieldInfo, object), List<(FieldInfo, object)>>.ReturnCollection(list);
			return null;
		}
		(FieldInfo, object)[] fieldValuesArray = list.ToArray();
		list = CollectionPool<(FieldInfo, object), List<(FieldInfo, object)>>.ReturnCollection(list);
		return InitializeObject;
		static bool FieldPassesFilter(FieldInfo fieldInfo)
		{
			return CustomAttributeExtensions.GetCustomAttribute<SerializeField>(fieldInfo) != null;
		}
		void InitializeObject(object obj)
		{
			for (int j = 0; j < fieldValuesArray.Length; j++)
			{
				var (fieldInfo2, value) = fieldValuesArray[j];
				fieldInfo2.SetValue(obj, value);
			}
		}
	}

	private void Awake()
	{
		serializedFieldsCollection.PurgeUnityPsuedoNullFields();
	}

	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			RoR2Application.onNextUpdate += delegate
			{
				EntityStateConfiguration.onEditorUpdatedConfigurationGlobal?.Invoke(this);
			};
		}
	}
}
