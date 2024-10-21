using RoR2;
using UnityEngine.Networking;

namespace EntityStates;

public class BaseSkillState : BaseState, ISkillState
{
	public GenericSkill activatorSkillSlot { get; set; }

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		this.Serialize(base.skillLocator, writer);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		this.Deserialize(base.skillLocator, reader);
	}

	public bool IsKeyDownAuthority()
	{
		return this.IsKeyDownAuthority(base.skillLocator, base.inputBank);
	}
}
