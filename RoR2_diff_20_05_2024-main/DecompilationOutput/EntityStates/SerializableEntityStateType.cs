using System;
using UnityEngine;

namespace EntityStates;

[Serializable]
public struct SerializableEntityStateType
{
	private Type _stateType;

	[SerializeField]
	private string _typeName;

	public string typeName
	{
		get
		{
			return _typeName;
		}
		private set
		{
			stateType = Type.GetType(value);
		}
	}

	public Type stateType
	{
		get
		{
			if (_stateType == null)
			{
				CacheStateType();
				return _stateType;
			}
			return _stateType;
		}
		set
		{
			_typeName = ((value != null && value.IsSubclassOf(typeof(EntityState))) ? value.AssemblyQualifiedName : "");
		}
	}

	public SerializableEntityStateType(string typeName)
	{
		_typeName = "";
		_stateType = null;
		this.typeName = typeName;
	}

	public SerializableEntityStateType(Type stateType)
	{
		_typeName = "";
		_stateType = null;
		this.stateType = stateType;
	}

	private void CacheStateType()
	{
		if (_typeName != null)
		{
			Type type = Type.GetType(_typeName);
			_stateType = ((type != null && type.IsSubclassOf(typeof(EntityState))) ? type : null);
		}
	}
}
