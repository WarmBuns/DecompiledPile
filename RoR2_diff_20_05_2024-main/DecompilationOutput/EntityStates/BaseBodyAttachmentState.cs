using RoR2;

namespace EntityStates;

public class BaseBodyAttachmentState : EntityState
{
	protected NetworkedBodyAttachment bodyAttachment { get; private set; }

	protected CharacterBody attachedBody { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		bodyAttachment = GetComponent<NetworkedBodyAttachment>();
		attachedBody = (bodyAttachment ? bodyAttachment.attachedBody : null);
	}
}
