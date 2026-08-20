namespace Project.Master.NoaMessagePack;

[MessagePackObject(False)]
public sealed class MNetherMapFloorsArray
{
	[Key(0)]
	public MNetherMapFloors[] elements; //Field offset: 0x10

	public MNetherMapFloorsArray() { }

}

