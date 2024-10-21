using System;
using System.Collections.Generic;
using Rewired;
using UnityEngine;

namespace RoR2;

public static class DefaultControllerMaps
{
	public delegate void LogFormatFunction(string inFormat, params object[] inParams);

	public const string defaultKeyboardMapString = "<?xml version=\"1.0\" encoding=\"utf-16\"?><KeyboardMap dataVersion=\"2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://guavaman.com/rewired http://guavaman.com/schemas/rewired/1.1/KeyboardMap.xsd\" xmlns=\"http://guavaman.com/rewired\"><sourceMapId>0</sourceMapId><categoryId>0</categoryId><layoutId>0</layoutId><name></name><hardwareGuid>00000000-0000-0000-0000-000000000000</hardwareGuid><enabled>false</enabled><buttonMaps><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>0</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>100</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>0</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>1</axisContribution><keyboardKeyCode>97</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>1</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>119</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>1</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>1</axisContribution><keyboardKeyCode>115</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>4</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>32</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>18</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>306</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>5</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>101</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>6</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>113</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>9</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>304</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>10</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>114</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>19</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>9</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap></buttonMaps></KeyboardMap>";

	public const string defaultMouseMapString = "<?xml version=\"1.0\" encoding=\"utf-16\"?><MouseMap dataVersion=\"2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://guavaman.com/rewired http://guavaman.com/schemas/rewired/1.1/MouseMap.xsd\" xmlns=\"http://guavaman.com/rewired\"><sourceMapId>0</sourceMapId><categoryId>0</categoryId><layoutId>0</layoutId><name></name><hardwareGuid>00000000-0000-0000-0000-000000000000</hardwareGuid><enabled>false</enabled><buttonMaps><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>7</actionId><elementType>1</elementType><elementIdentifierId>3</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>8</actionId><elementType>1</elementType><elementIdentifierId>4</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>28</actionId><elementType>1</elementType><elementIdentifierId>5</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap></buttonMaps><axisMaps><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>2</actionId><elementType>0</elementType><elementIdentifierId>0</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>3</actionId><elementType>0</elementType><elementIdentifierId>1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap></axisMaps></MouseMap>";

	public static readonly Guid gamepadTemplateGuid = new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5");

	public static readonly Guid xboneControllerGuid = new Guid("19002688-7406-4f4a-8340-8d25335406c8");

	public static readonly Guid xbox360ControllerGuid = new Guid("d74a350e-fe8b-4e9e-bbcd-efff16d34115");

	public static readonly Guid switchDualGuid = new Guid("521b808c-0248-4526-bc10-f1d16ee76bf1");

	public static readonly Guid switchHandheldGuid = new Guid("1fbdd13b-0795-4173-8a95-a2a75de9d204");

	public static readonly Guid switchProGuid = new Guid("7bf3154b-9db8-4d52-950f-cd0eed8a5819");

	public static readonly Guid PS4Guid = new Guid("cd9718bf-a87a-44bc-8716-60a0def28a9f");

	public static readonly Guid PS5Guid = new Guid("5286706d-19b4-4a45-b635-207ce78d8394");

	public static readonly string xbox360ControllerName = "Xbox 360 Controller";

	public static readonly string xboxOneControllerName = "Xbox One Controller";

	public static readonly string dualshock4ControllerName = "Sony DualShock 4";

	public static readonly string dualshock4ControllerNameAlt = "PlayStation Controller";

	public static readonly string dualSenseController = "Sony DualSense";

	public static readonly string switchControllerName = "Nintendo Controller";

	public static readonly string switchProControllerName = "Nintendo Switch Pro Controller";

	public static readonly string switchProControllerNameAlt = "Pro Controller";

	public static readonly string stadiaControllerName = "Stadia Controller";

	public static readonly KeyboardMap defaultKeyboardMap = (KeyboardMap)ControllerMap.CreateFromXml(ControllerType.Keyboard, "<?xml version=\"1.0\" encoding=\"utf-16\"?><KeyboardMap dataVersion=\"2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://guavaman.com/rewired http://guavaman.com/schemas/rewired/1.1/KeyboardMap.xsd\" xmlns=\"http://guavaman.com/rewired\"><sourceMapId>0</sourceMapId><categoryId>0</categoryId><layoutId>0</layoutId><name></name><hardwareGuid>00000000-0000-0000-0000-000000000000</hardwareGuid><enabled>false</enabled><buttonMaps><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>0</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>100</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>0</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>1</axisContribution><keyboardKeyCode>97</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>1</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>119</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>1</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>1</axisContribution><keyboardKeyCode>115</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>4</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>32</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>18</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>306</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>5</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>101</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>6</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>113</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>9</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>304</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>10</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>114</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>19</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>9</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap></buttonMaps></KeyboardMap>");

	public static readonly MouseMap defaultMouseMap = (MouseMap)ControllerMap.CreateFromXml(ControllerType.Mouse, "<?xml version=\"1.0\" encoding=\"utf-16\"?><MouseMap dataVersion=\"2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://guavaman.com/rewired http://guavaman.com/schemas/rewired/1.1/MouseMap.xsd\" xmlns=\"http://guavaman.com/rewired\"><sourceMapId>0</sourceMapId><categoryId>0</categoryId><layoutId>0</layoutId><name></name><hardwareGuid>00000000-0000-0000-0000-000000000000</hardwareGuid><enabled>false</enabled><buttonMaps><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>7</actionId><elementType>1</elementType><elementIdentifierId>3</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>8</actionId><elementType>1</elementType><elementIdentifierId>4</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>28</actionId><elementType>1</elementType><elementIdentifierId>5</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap></buttonMaps><axisMaps><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>2</actionId><elementType>0</elementType><elementIdentifierId>0</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>3</actionId><elementType>0</elementType><elementIdentifierId>1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap></axisMaps></MouseMap>");

	private static Dictionary<Guid, string> _DefaultJoystickMaps_Xml = new Dictionary<Guid, string>();

	private static bool _FilledDefaultJoystickMaps = false;

	public static JoystickMap DefaultJoystickMap
	{
		get
		{
			FillDefaultJoystickMaps();
			if (_DefaultJoystickMaps_Xml.ContainsKey(PlatformManager.DefaultControllerGuid))
			{
				string xmlString = _DefaultJoystickMaps_Xml[PlatformManager.DefaultControllerGuid];
				return ControllerMap.CreateFromXml(ControllerType.Joystick, xmlString) as JoystickMap;
			}
			return null;
		}
	}

	public static bool RewiredLoggingEnabled()
	{
		return false;
	}

	public static void RewiredLogLineForce(LogFormatFunction inLogFcn, string inFormat, params object[] inParams)
	{
		if (inParams != null && inParams.Length != 0)
		{
			inLogFcn(inFormat, inParams);
		}
		else
		{
			inLogFcn(inFormat);
		}
	}

	public static void RewiredLogLine(LogFormatFunction inLogFcn, string inFormat, params object[] inParams)
	{
	}

	public static void RewiredDebugLog(string inFormat, params object[] inParams)
	{
		RewiredLogLine(Debug.LogFormat, inFormat, inParams);
	}

	public static void RewiredDebugWarn(string inFormat, params object[] inParams)
	{
		RewiredLogLine(Debug.LogWarningFormat, inFormat, inParams);
	}

	public static void RewiredDebugError(string inFormat, params object[] inParams)
	{
		RewiredLogLineForce(Debug.LogErrorFormat, inFormat, inParams);
	}

	public static void RewiredDebugLogForce(string inFormat, params object[] inParams)
	{
		RewiredLogLineForce(Debug.LogFormat, inFormat, inParams);
	}

	public static void RewiredDebugWarnForce(string inFormat, params object[] inParams)
	{
		RewiredLogLineForce(Debug.LogWarningFormat, inFormat, inParams);
	}

	private static void FillDefaultJoystickMaps()
	{
		if (_FilledDefaultJoystickMaps)
		{
			return;
		}
		RewiredDebugLog("FillDefaultJoysticksMap");
		if (ReInput.mapping != null)
		{
			JoystickMap joystickMapInstance = ReInput.mapping.GetJoystickMapInstance(gamepadTemplateGuid, 0, 0);
			JoystickMap joystickMapInstance2 = ReInput.mapping.GetJoystickMapInstance(xboneControllerGuid, 0, 0);
			JoystickMap joystickMapInstance3 = ReInput.mapping.GetJoystickMapInstance(xbox360ControllerGuid, 0, 0);
			JoystickMap joystickMapInstance4 = ReInput.mapping.GetJoystickMapInstance(PS4Guid, 0, 0);
			JoystickMap joystickMapInstance5 = ReInput.mapping.GetJoystickMapInstance(PS5Guid, 0, 0);
			JoystickMap joystickMapInstance6 = ReInput.mapping.GetJoystickMapInstance(switchProGuid, 0, 0);
			JoystickMap joystickMapInstance7 = ReInput.mapping.GetJoystickMapInstance(switchDualGuid, 0, 0);
			JoystickMap joystickMapInstance8 = ReInput.mapping.GetJoystickMapInstance(switchHandheldGuid, 0, 0);
			string value = joystickMapInstance.ToXmlString();
			string value2 = joystickMapInstance2.ToXmlString();
			string value3 = joystickMapInstance3.ToXmlString();
			string value4 = joystickMapInstance4.ToXmlString();
			string value5 = joystickMapInstance5.ToXmlString();
			string value6 = joystickMapInstance6.ToXmlString();
			string value7 = joystickMapInstance7.ToXmlString();
			string value8 = joystickMapInstance8.ToXmlString();
			_DefaultJoystickMaps_Xml.Add(gamepadTemplateGuid, value);
			if (joystickMapInstance2.buttonMapCount > 0)
			{
				_DefaultJoystickMaps_Xml.Add(xboneControllerGuid, value2);
			}
			_DefaultJoystickMaps_Xml.Add(xbox360ControllerGuid, value3);
			_DefaultJoystickMaps_Xml.Add(PS4Guid, value4);
			_DefaultJoystickMaps_Xml.Add(PS5Guid, value5);
			_DefaultJoystickMaps_Xml.Add(switchProGuid, value6);
			_DefaultJoystickMaps_Xml.Add(switchDualGuid, value7);
			_DefaultJoystickMaps_Xml.Add(switchHandheldGuid, value8);
			_FilledDefaultJoystickMaps = true;
		}
	}

	public static Dictionary<Guid, JoystickMap> GetDefaultJoystickMaps()
	{
		FillDefaultJoystickMaps();
		Dictionary<Guid, JoystickMap> dictionary = new Dictionary<Guid, JoystickMap>();
		foreach (KeyValuePair<Guid, string> item in _DefaultJoystickMaps_Xml)
		{
			JoystickMap value = ControllerMap.CreateFromXml(ControllerType.Joystick, item.Value) as JoystickMap;
			dictionary.Add(item.Key, value);
		}
		return dictionary;
	}

	public static KeyboardMap GetDefaultKeyboardMap()
	{
		return ControllerMap.CreateFromXml(ControllerType.Keyboard, "<?xml version=\"1.0\" encoding=\"utf-16\"?><KeyboardMap dataVersion=\"2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://guavaman.com/rewired http://guavaman.com/schemas/rewired/1.1/KeyboardMap.xsd\" xmlns=\"http://guavaman.com/rewired\"><sourceMapId>0</sourceMapId><categoryId>0</categoryId><layoutId>0</layoutId><name></name><hardwareGuid>00000000-0000-0000-0000-000000000000</hardwareGuid><enabled>false</enabled><buttonMaps><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>0</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>100</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>0</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>1</axisContribution><keyboardKeyCode>97</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>1</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>119</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>1</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>1</axisContribution><keyboardKeyCode>115</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>4</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>32</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>18</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>306</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>5</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>101</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>6</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>113</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>9</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>304</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>10</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>114</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>19</actionId><elementType>1</elementType><elementIdentifierId>-1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>9</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap></buttonMaps></KeyboardMap>") as KeyboardMap;
	}

	public static MouseMap GetDefaultMoustMap()
	{
		return ControllerMap.CreateFromXml(ControllerType.Mouse, "<?xml version=\"1.0\" encoding=\"utf-16\"?><MouseMap dataVersion=\"2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://guavaman.com/rewired http://guavaman.com/schemas/rewired/1.1/MouseMap.xsd\" xmlns=\"http://guavaman.com/rewired\"><sourceMapId>0</sourceMapId><categoryId>0</categoryId><layoutId>0</layoutId><name></name><hardwareGuid>00000000-0000-0000-0000-000000000000</hardwareGuid><enabled>false</enabled><buttonMaps><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>7</actionId><elementType>1</elementType><elementIdentifierId>3</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>8</actionId><elementType>1</elementType><elementIdentifierId>4</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>28</actionId><elementType>1</elementType><elementIdentifierId>5</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap></buttonMaps><axisMaps><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>2</actionId><elementType>0</elementType><elementIdentifierId>0</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap><ActionElementMap><actionCategoryId>0</actionCategoryId><actionId>3</actionId><elementType>0</elementType><elementIdentifierId>1</elementIdentifierId><axisRange>0</axisRange><invert>false</invert><axisContribution>0</axisContribution><keyboardKeyCode>0</keyboardKeyCode><modifierKey1>0</modifierKey1><modifierKey2>0</modifierKey2><modifierKey3>0</modifierKey3><enabled>true</enabled></ActionElementMap></axisMaps></MouseMap>") as MouseMap;
	}

	public static JoystickMap GetDefaultJoystickMap(Guid inControllerGuid)
	{
		FillDefaultJoystickMaps();
		if (_DefaultJoystickMaps_Xml.ContainsKey(inControllerGuid))
		{
			string xmlString = _DefaultJoystickMaps_Xml[inControllerGuid];
			return ControllerMap.CreateFromXml(ControllerType.Joystick, xmlString) as JoystickMap;
		}
		RewiredDebugWarn($"GetDefaultControllerMap: Failed to find guid {inControllerGuid}...");
		if (inControllerGuid == xboneControllerGuid && _DefaultJoystickMaps_Xml.ContainsKey(xbox360ControllerGuid))
		{
			RewiredDebugWarn($"GetDefaultControllerMap: Failed to find guid {inControllerGuid}... returning Xbox360 mapping (XInput detected)");
			string xmlString2 = _DefaultJoystickMaps_Xml[xbox360ControllerGuid];
			return ControllerMap.CreateFromXml(ControllerType.Joystick, xmlString2) as JoystickMap;
		}
		if (inControllerGuid == xbox360ControllerGuid && _DefaultJoystickMaps_Xml.ContainsKey(xboneControllerGuid))
		{
			RewiredDebugWarn($"GetDefaultControllerMap: Failed to find guid {inControllerGuid}... returning Xbone mapping (XInput detected)");
			string xmlString3 = _DefaultJoystickMaps_Xml[xboneControllerGuid];
			return ControllerMap.CreateFromXml(ControllerType.Joystick, xmlString3) as JoystickMap;
		}
		if (_DefaultJoystickMaps_Xml.ContainsKey(PlatformManager.DefaultControllerGuid))
		{
			RewiredDebugWarn($"GetDefaultControllerMap: Failed to find guid {inControllerGuid}... returning default");
			string xmlString4 = _DefaultJoystickMaps_Xml[PlatformManager.DefaultControllerGuid];
			return ControllerMap.CreateFromXml(ControllerType.Joystick, xmlString4) as JoystickMap;
		}
		RewiredDebugError($"GetDefaultControllerMap: Failed to find guid {inControllerGuid}... nor default {PlatformManager.DefaultControllerGuid}. Returning null");
		return null;
	}

	public static bool HardwareGuidsMatch(Guid inA, Guid inB)
	{
		if (inA == inB)
		{
			RewiredDebugLog("HardwareGuidsMatch: TRUE, direct match: {0} --> {1}", inA.ToString(), inB.ToString());
			return true;
		}
		RewiredDebugLog("HardwareGuidsMatch: FALSE, no match: {0} --> {1}", inA.ToString(), inB.ToString());
		return false;
	}

	public static Guid LocalHardwareGuidRemap(Guid inHardwareGuid)
	{
		Guid result = inHardwareGuid;
		RewiredDebugLog("LocalHardwareGuidRemap: {0} --> {1}", inHardwareGuid.ToString(), result.ToString());
		return result;
	}

	[ConCommand(commandName = "export_default_controller_maps", flags = ConVarFlags.None, helpText = "Prints all default Rewired ControllerMaps.")]
	public static void CCExportDefaultControllerMaps(ConCommandArgs args)
	{
	}
}
