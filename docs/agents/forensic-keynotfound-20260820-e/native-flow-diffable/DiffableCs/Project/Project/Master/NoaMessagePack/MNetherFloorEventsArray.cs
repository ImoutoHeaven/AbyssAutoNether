namespace Project.Master.NoaMessagePack;

[MessagePackObject(False)]
public sealed class MNetherFloorEventsArray
{
	[Key(0)]
	public MNetherFloorEvents[] elements; //Field offset: 0x10

	public MNetherFloorEventsArray() { }

}

