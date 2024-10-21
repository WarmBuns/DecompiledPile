using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EntityStates;
using HG;
using HG.GeneralSerializer;
using RoR2;
using UnityEngine;
using UnityEngine.Serialization;

public class EntityStateManager : ScriptableObject, ISerializationCallbackReceiver
{
	[Serializable]
	private class StateInfo
	{
		[Serializable]
		public class Field
		{
			public enum ValueType
			{
				Invalid,
				Int,
				Float,
				String,
				Object,
				Bool,
				AnimationCurve,
				Vector3
			}

			[NonSerialized]
			public StateInfo owner;

			[SerializeField]
			private string _fieldName;

			[SerializeField]
			private ValueType _valueType;

			[SerializeField]
			private int _intValue;

			[SerializeField]
			private float _floatValue;

			[SerializeField]
			private string _stringValue;

			[SerializeField]
			private UnityEngine.Object _objectValue;

			[SerializeField]
			private AnimationCurve _animationCurveValue;

			[SerializeField]
			private Vector3 _vector3Value;

			public ValueType valueType => _valueType;

			public int intValue => _intValue;

			public bool boolValue => _intValue != 0;

			public float floatValue => _floatValue;

			public string stringValue => _stringValue;

			public UnityEngine.Object objectValue => _objectValue;

			public AnimationCurve animationCurveValue => _animationCurveValue;

			public Vector3 vector3Value => _vector3Value;

			public object valueAsSystemObject
			{
				get
				{
					switch (_valueType)
					{
					case ValueType.Invalid:
						return null;
					case ValueType.Int:
						return intValue;
					case ValueType.Float:
						return floatValue;
					case ValueType.String:
						return stringValue;
					case ValueType.Object:
						if (!objectValue)
						{
							return null;
						}
						return objectValue;
					case ValueType.Bool:
						return boolValue;
					case ValueType.AnimationCurve:
						return animationCurveValue;
					case ValueType.Vector3:
						return vector3Value;
					default:
						return null;
					}
				}
			}

			public Field(string fieldName)
			{
				_fieldName = fieldName;
			}

			public void SetFieldInfo(FieldInfo fieldInfo)
			{
				_fieldName = fieldInfo.Name;
				ValueType valueType = ValueType.Invalid;
				Type fieldType = fieldInfo.FieldType;
				if (fieldType == typeof(int))
				{
					valueType = ValueType.Int;
				}
				else if (fieldType == typeof(float))
				{
					valueType = ValueType.Float;
				}
				else if (fieldType == typeof(string))
				{
					valueType = ValueType.String;
				}
				else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
				{
					valueType = ValueType.Object;
				}
				else if (fieldType == typeof(bool))
				{
					valueType = ValueType.Bool;
				}
				else if (fieldType == typeof(AnimationCurve))
				{
					valueType = ValueType.AnimationCurve;
				}
				else if (fieldType == typeof(Vector3))
				{
					valueType = ValueType.Vector3;
				}
				if (_valueType != valueType)
				{
					ResetValues();
					_valueType = valueType;
				}
			}

			public bool MatchesFieldInfo(FieldInfo fieldInfo)
			{
				if (fieldInfo == null)
				{
					return false;
				}
				Type fieldType = fieldInfo.FieldType;
				switch (_valueType)
				{
				case ValueType.Invalid:
					return false;
				case ValueType.Int:
					return fieldType.IsAssignableFrom(typeof(int));
				case ValueType.Float:
					return fieldType.IsAssignableFrom(typeof(float));
				case ValueType.String:
					return fieldType.IsAssignableFrom(typeof(string));
				case ValueType.Object:
					if (!(_objectValue == null))
					{
						return fieldType.IsAssignableFrom(_objectValue.GetType());
					}
					return true;
				case ValueType.Bool:
					return fieldType.IsAssignableFrom(typeof(bool));
				case ValueType.AnimationCurve:
					return fieldType.IsAssignableFrom(typeof(AnimationCurve));
				case ValueType.Vector3:
					return fieldType.IsAssignableFrom(typeof(Vector3));
				default:
					return false;
				}
			}

			public void Apply(FieldInfo fieldInfo, object instance)
			{
				fieldInfo.SetValue(instance, valueAsSystemObject);
			}

			public void ResetValues()
			{
				_intValue = 0;
				_floatValue = 0f;
				_stringValue = null;
				_objectValue = null;
				_animationCurveValue = null;
				_vector3Value = Vector3.zero;
			}

			public void SetValue(int value)
			{
				ResetValues();
				_valueType = ValueType.Int;
				_intValue = value;
			}

			public void SetValue(float value)
			{
				ResetValues();
				_valueType = ValueType.Float;
				_floatValue = value;
			}

			public void SetValue(string value)
			{
				ResetValues();
				_valueType = ValueType.String;
				_stringValue = value;
			}

			public void SetValue(UnityEngine.Object value)
			{
				ResetValues();
				_valueType = ValueType.Object;
				_objectValue = value;
			}

			public void SetValue(bool value)
			{
				ResetValues();
				_valueType = ValueType.Bool;
				_intValue = (value ? 1 : 0);
			}

			public void SetValue(AnimationCurve value)
			{
				ResetValues();
				_valueType = ValueType.AnimationCurve;
				_animationCurveValue = value;
			}

			public void SetValue(Vector3 value)
			{
				ResetValues();
				_valueType = ValueType.Vector3;
				_vector3Value = value;
			}

			public string GetFieldName()
			{
				return _fieldName;
			}

			public FieldInfo GetFieldInfo()
			{
				return owner?.serializedType.stateType?.GetField(_fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			}
		}

		private struct FieldValuePair
		{
			public FieldInfo fieldInfo;

			public object value;
		}

		public SerializableEntityStateType serializedType;

		[FormerlySerializedAs("stateStaticFieldList")]
		[SerializeField]
		private List<Field> stateFieldList = new List<Field>();

		private const BindingFlags defaultInstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

		private const BindingFlags defaultStaticBindingFlags = BindingFlags.Static | BindingFlags.Public;

		private static bool FieldHasSerializeAttribute(FieldInfo fieldInfo)
		{
			return CustomAttributeExtensions.GetCustomAttributes<SerializeField>(fieldInfo, inherit: true).Any();
		}

		private static bool FieldLacksNonSerializedAttribute(FieldInfo fieldInfo)
		{
			return !CustomAttributeExtensions.GetCustomAttributes<NonSerializedAttribute>(fieldInfo, inherit: true).Any();
		}

		public void SetStateType(Type stateType)
		{
			serializedType.stateType = stateType;
			stateType = serializedType.stateType;
			if (stateType == null)
			{
				return;
			}
			IEnumerable<FieldInfo> first = stateType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(FieldHasSerializeAttribute);
			IEnumerable<FieldInfo> second = (from fieldInfo in stateType.GetFields(BindingFlags.Static | BindingFlags.Public)
				where fieldInfo.DeclaringType == stateType
				select fieldInfo).Where(FieldLacksNonSerializedAttribute);
			List<FieldInfo> list = first.Concat(second).ToList();
			Dictionary<FieldInfo, Field> dictionary = new Dictionary<FieldInfo, Field>();
			foreach (FieldInfo fieldInfo2 in list)
			{
				Field field = stateFieldList.Find((Field item) => item.GetFieldName() == fieldInfo2.Name);
				if (field == null)
				{
					Debug.LogFormat("Could not find field {0}.{1}. Initializing new field.", stateType.Name, fieldInfo2.Name);
					field = new Field(fieldInfo2.Name);
				}
				dictionary[fieldInfo2] = field;
			}
			stateFieldList.Clear();
			foreach (FieldInfo item in list)
			{
				Field field2 = dictionary[item];
				field2.owner = this;
				field2.SetFieldInfo(item);
				stateFieldList.Add(field2);
			}
		}

		public void RefreshStateType()
		{
			SetStateType(serializedType.stateType);
		}

		public void ApplyStatic()
		{
			Type stateType = serializedType.stateType;
			if (!(stateType != null))
			{
				return;
			}
			foreach (Field stateField in stateFieldList)
			{
				FieldInfo field = stateType.GetField(stateField.GetFieldName(), BindingFlags.Static | BindingFlags.Public);
				if (stateField.MatchesFieldInfo(field) && field.IsStatic)
				{
					stateField.Apply(field, null);
				}
			}
		}

		public Action<EntityState> GenerateInstanceFieldInitializerDelegate()
		{
			Type stateType = serializedType.stateType;
			if (stateType == null)
			{
				return null;
			}
			List<FieldValuePair> list = new List<FieldValuePair>();
			for (int i = 0; i < stateFieldList.Count; i++)
			{
				Field field = stateFieldList[i];
				FieldValuePair fieldValuePair = default(FieldValuePair);
				fieldValuePair.fieldInfo = stateType.GetField(field.GetFieldName(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				fieldValuePair.value = field.valueAsSystemObject;
				FieldValuePair item = fieldValuePair;
				if (!(item.fieldInfo == null))
				{
					list.Add(item);
				}
			}
			FieldValuePair[] fieldValuePairs = list.ToArray();
			if (fieldValuePairs.Length == 0)
			{
				return null;
			}
			return delegate(EntityState entityState)
			{
				for (int j = 0; j < fieldValuePairs.Length; j++)
				{
					FieldValuePair fieldValuePair2 = fieldValuePairs[j];
					fieldValuePair2.fieldInfo.SetValue(entityState, fieldValuePair2.value);
				}
			};
		}

		public Field FindField(string fieldName)
		{
			return stateFieldList.Find((Field value) => value.GetFieldName() == fieldName);
		}

		public bool IsValid()
		{
			return serializedType.stateType != null;
		}

		public IList<Field> GetFields()
		{
			return stateFieldList.AsReadOnly();
		}
	}

	[SerializeField]
	private List<StateInfo> stateInfoList = new List<StateInfo>();

	[HideInInspector]
	[SerializeField]
	private string endMarker = "GIT_END";

	private static readonly Dictionary<Type, Action<EntityState>> instanceFieldInitializers = new Dictionary<Type, Action<EntityState>>();

	private StateInfo GetStateInfo(Type stateType)
	{
		if (stateType == null || !stateType.IsSubclassOf(typeof(EntityState)))
		{
			return null;
		}
		StateInfo stateInfo = stateInfoList.Find((StateInfo currentItem) => currentItem.serializedType.stateType == stateType);
		if (stateInfo == null)
		{
			stateInfo = new StateInfo();
			stateInfo.SetStateType(stateType);
			stateInfoList.Add(stateInfo);
		}
		return stateInfo;
	}

	private void ApplyStatic()
	{
		foreach (StateInfo stateInfo in stateInfoList)
		{
			stateInfo.ApplyStatic();
		}
	}

	public void Initialize()
	{
	}

	private static void SetEntityStateConfigurations(EntityStateConfiguration[] newEntityStateConfigurations)
	{
		EntityStateConfiguration[] array = ArrayUtils.Clone(newEntityStateConfigurations);
		Array.Sort(array, (EntityStateConfiguration a, EntityStateConfiguration b) => a.name.CompareTo(b.name));
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ApplyStatic();
		}
		instanceFieldInitializers.Clear();
		foreach (EntityStateConfiguration entityStateConfiguration in array)
		{
			Action<object> action = entityStateConfiguration.BuildInstanceInitializer();
			if (action != null)
			{
				instanceFieldInitializers[(Type)entityStateConfiguration.targetType] = action;
			}
		}
	}

	void ISerializationCallbackReceiver.OnBeforeSerialize()
	{
		foreach (StateInfo stateInfo in stateInfoList)
		{
			stateInfo.RefreshStateType();
		}
		stateInfoList.RemoveAll((StateInfo stateInfo) => !stateInfo.IsValid());
	}

	void ISerializationCallbackReceiver.OnAfterDeserialize()
	{
		foreach (StateInfo stateInfo in stateInfoList)
		{
			stateInfo.RefreshStateType();
		}
	}

	private void GenerateInstanceFieldInitializers()
	{
		instanceFieldInitializers.Clear();
		foreach (StateInfo stateInfo in stateInfoList)
		{
			Type stateType = stateInfo.serializedType.stateType;
			if (!(stateType == null))
			{
				Action<EntityState> action = stateInfo.GenerateInstanceFieldInitializerDelegate();
				if (action != null)
				{
					instanceFieldInitializers.Add(stateType, action);
				}
			}
		}
	}

	public static void InitializeStateFields(EntityState entityState)
	{
		instanceFieldInitializers.TryGetValue(entityState.GetType(), out var value);
		value?.Invoke(entityState);
	}

	[ContextMenu("Migrate to individual assets")]
	public void MigrateToEntityStateTypes()
	{
		List<StateInfo> list = new List<StateInfo>();
		foreach (StateInfo stateInfo in stateInfoList)
		{
			if (MigrateToEntityStateType(stateInfo))
			{
				list.Add(stateInfo);
			}
		}
		foreach (StateInfo item in list)
		{
			_ = item;
		}
		foreach (Type item2 in from t in typeof(EntityState).Assembly.GetTypes()
			where typeof(EntityState).IsAssignableFrom(t)
			select t)
		{
			GetOrCreateEntityStateSerializer(item2);
		}
	}

	private EntityStateConfiguration GetOrCreateEntityStateSerializer(Type type)
	{
		return null;
	}

	private bool MigrateToEntityStateType(StateInfo stateInfo)
	{
		Type stateType = stateInfo.serializedType.stateType;
		if (stateType == null)
		{
			Debug.LogWarningFormat("Could not migrate type named \"{0}\": Type could not be found.", typeof(SerializableEntityStateType).GetField("_typeName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stateInfo.serializedType));
			return false;
		}
		EntityStateConfiguration orCreateEntityStateSerializer = GetOrCreateEntityStateSerializer(stateType);
		foreach (StateInfo.Field field2 in stateInfo.GetFields())
		{
			string fieldName = field2.GetFieldName();
			FieldInfo field = stateType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			ref SerializedField orCreateField = ref orCreateEntityStateSerializer.serializedFieldsCollection.GetOrCreateField(fieldName);
			orCreateField.fieldName = fieldName;
			try
			{
				orCreateField.fieldValue.SetValue(field, field2.valueAsSystemObject);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		EditorUtil.SetDirty(orCreateEntityStateSerializer);
		return true;
	}
}
