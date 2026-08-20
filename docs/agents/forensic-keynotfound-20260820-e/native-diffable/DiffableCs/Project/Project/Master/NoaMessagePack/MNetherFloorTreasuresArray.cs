namespace Project.Master.NoaMessagePack;

[MessagePackObject(False)]
public sealed class MNetherFloorTreasuresArray
{
	[Key(0)]
	public MNetherFloorTreasures[] elements; //Field offset: 0x10

	public MNetherFloorTreasuresArray() { }

}

