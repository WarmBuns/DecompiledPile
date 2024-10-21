using System;
using System.Collections.Generic;

namespace RoR2;

internal sealed class EOSServerManager : ServerManagerBase<EOSServerManager>, IDisposable
{
	private Dictionary<string, string> KeyValue = new Dictionary<string, string>();

	public EOSServerManager()
	{
		Run.onServerRunSetRuleBookGlobal += base.OnServerRunSetRuleBookGlobal;
		PreGameController.onPreGameControllerSetRuleBookServerGlobal += base.OnPreGameControllerSetRuleBookServerGlobal;
		ruleBookKvHelper = new KeyValueSplitter("ruleBook", 2048, 2048, SetKey);
		modListKvHelper = new KeyValueSplitter(NetworkModCompatibilityHelper.steamworksGameserverRulesBaseName, 2048, 2048, SetKey);
		modListKvHelper.SetValue(NetworkModCompatibilityHelper.steamworksGameserverGameRulesValue);
		UpdateServerRuleBook();
	}

	public void SetKey(string Key, string Value)
	{
		KeyValue[Key] = Value;
	}
}
