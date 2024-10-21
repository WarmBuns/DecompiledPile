using RoR2.Skills;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor.Weapon;

public class EnterSwingMelee : BaseState, SteppedSkillDef.IStepSetter
{
	public int step;

	void SteppedSkillDef.IStepSetter.SetStep(int i)
	{
		step = i;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write((byte)step);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		step = reader.ReadByte();
	}

	public override void OnEnter()
	{
		base.OnEnter();
		switch (step)
		{
		case 0:
			outer.SetNextState(new SwingMelee1());
			break;
		case 1:
			outer.SetNextState(new SwingMelee2());
			break;
		case 2:
			outer.SetNextState(new SwingMelee3());
			break;
		default:
			outer.SetNextState(new SwingMelee1());
			break;
		}
	}
}
