namespace Project.Nether.FloorSelection;

public sealed class NetherFloorModel
{
	[CompilerGenerated]
	private sealed class <>c
	{
		public static readonly <>c <>9; //Field offset: 0x0
		public static Func<MNetherMapFloors, NetherMapFloorEntity, Boolean> <>9__57_0; //Field offset: 0x8
		public static Func<MNetherFloorRestricts, NetherMapFloorEntity, Boolean> <>9__57_1; //Field offset: 0x10
		public static Func<MNetherFloorBattles, Int64, Boolean> <>9__57_2; //Field offset: 0x18
		public static Func<MNetherBattleStages, Int64, Boolean> <>9__57_3; //Field offset: 0x20

		private static <>c() { }

		public <>c() { }

		internal bool <CreateModel>b__57_0(MNetherMapFloors masterData, NetherMapFloorEntity sEntity) { }

		internal bool <CreateModel>b__57_1(MNetherFloorRestricts masterData, NetherMapFloorEntity sEntity) { }

		internal bool <CreateModel>b__57_2(MNetherFloorBattles masterData, long mNetherMapFloorId) { }

		internal bool <CreateModel>b__57_3(MNetherBattleStages masterData, long mNetherBattleStageId) { }

	}

	[CompilerGenerated]
	private readonly NetherFloorSizeType <SizeType>k__BackingField; //Field offset: 0x10
	[CompilerGenerated]
	private readonly NetherFloorType <FloorType>k__BackingField; //Field offset: 0x14
	[CompilerGenerated]
	private readonly long <MNetherMapFloorId>k__BackingField; //Field offset: 0x18
	[CompilerGenerated]
	private readonly long <ExtendId>k__BackingField; //Field offset: 0x20
	[CompilerGenerated]
	private readonly int <FloorLevel>k__BackingField; //Field offset: 0x28
	[CompilerGenerated]
	private readonly int <FloorIndex>k__BackingField; //Field offset: 0x2C
	[CompilerGenerated]
	private readonly int <FloorPosition>k__BackingField; //Field offset: 0x30
	[CompilerGenerated]
	private readonly Int64[] <MNetherMapFloorPrevIds>k__BackingField; //Field offset: 0x38
	[CompilerGenerated]
	private readonly List<NetherFloorModel> <PrevFloorModelList>k__BackingField; //Field offset: 0x40
	[CompilerGenerated]
	private readonly List<NetherFloorModel> <NextFloorModelList>k__BackingField; //Field offset: 0x48
	[CompilerGenerated]
	private readonly bool <IsSecretFloor>k__BackingField; //Field offset: 0x50
	[CompilerGenerated]
	private bool <IsUnlocked>k__BackingField; //Field offset: 0x51
	[CompilerGenerated]
	private readonly int <RecommendPower>k__BackingField; //Field offset: 0x54

	public int ApiFloorIndex
	{
		 get { } //Length: 4
	}

	public int ApiFloorLevel
	{
		 get { } //Length: 4
	}

	public private long ExtendId
	{
		[CompilerGenerated]
		 get { } //Length: 5
		[CompilerGenerated]
		private set { } //Length: 5
	}

	public private int FloorIndex
	{
		[CompilerGenerated]
		 get { } //Length: 4
		[CompilerGenerated]
		private set { } //Length: 4
	}

	public private int FloorLevel
	{
		[CompilerGenerated]
		 get { } //Length: 4
		[CompilerGenerated]
		private set { } //Length: 4
	}

	public private int FloorPosition
	{
		[CompilerGenerated]
		 get { } //Length: 4
		[CompilerGenerated]
		private set { } //Length: 4
	}

	public private NetherFloorType FloorType
	{
		[CompilerGenerated]
		 get { } //Length: 4
		[CompilerGenerated]
		private set { } //Length: 4
	}

	public private bool IsSecretFloor
	{
		[CompilerGenerated]
		 get { } //Length: 5
		[CompilerGenerated]
		private set { } //Length: 4
	}

	public private bool IsUnlocked
	{
		[CompilerGenerated]
		 get { } //Length: 5
		[CompilerGenerated]
		private set { } //Length: 4
	}

	public private long MNetherMapFloorId
	{
		[CompilerGenerated]
		 get { } //Length: 5
		[CompilerGenerated]
		private set { } //Length: 5
	}

	public private Int64[] MNetherMapFloorPrevIds
	{
		[CompilerGenerated]
		 get { } //Length: 5
		[CompilerGenerated]
		private set { } //Length: 13
	}

	public private List<NetherFloorModel> NextFloorModelList
	{
		[CompilerGenerated]
		 get { } //Length: 5
		[CompilerGenerated]
		private set { } //Length: 13
	}

	public private List<NetherFloorModel> PrevFloorModelList
	{
		[CompilerGenerated]
		 get { } //Length: 5
		[CompilerGenerated]
		private set { } //Length: 13
	}

	public private int RecommendPower
	{
		[CompilerGenerated]
		 get { } //Length: 4
		[CompilerGenerated]
		private set { } //Length: 4
	}

	public private NetherFloorSizeType SizeType
	{
		[CompilerGenerated]
		 get { } //Length: 97
		[CompilerGenerated]
		private set { } //Length: 4
	}

	public NetherFloorModel() { }

	public static NetherFloorModel CreateModel(int index, NetherMapFloorEntity entity) { }

	public int get_ApiFloorIndex() { }

	public int get_ApiFloorLevel() { }

	[CompilerGenerated]
	public long get_ExtendId() { }

	[CompilerGenerated]
	public int get_FloorIndex() { }

	[CompilerGenerated]
	public int get_FloorLevel() { }

	[CompilerGenerated]
	public int get_FloorPosition() { }

	[CompilerGenerated]
	public NetherFloorType get_FloorType() { }

	[CompilerGenerated]
	public bool get_IsSecretFloor() { }

	[CompilerGenerated]
	public bool get_IsUnlocked() { }

	[CompilerGenerated]
	public long get_MNetherMapFloorId() { }

	[CompilerGenerated]
	public Int64[] get_MNetherMapFloorPrevIds() { }

	[CompilerGenerated]
	public List<NetherFloorModel> get_NextFloorModelList() { }

	[CompilerGenerated]
	public List<NetherFloorModel> get_PrevFloorModelList() { }

	[CompilerGenerated]
	public int get_RecommendPower() { }

	[CompilerGenerated]
	public NetherFloorSizeType get_SizeType() { }

	[CompilerGenerated]
	private void set_ExtendId(long value) { }

	[CompilerGenerated]
	private void set_FloorIndex(int value) { }

	[CompilerGenerated]
	private void set_FloorLevel(int value) { }

	[CompilerGenerated]
	private void set_FloorPosition(int value) { }

	[CompilerGenerated]
	private void set_FloorType(NetherFloorType value) { }

	[CompilerGenerated]
	private void set_IsSecretFloor(bool value) { }

	[CompilerGenerated]
	private void set_IsUnlocked(bool value) { }

	[CompilerGenerated]
	private void set_MNetherMapFloorId(long value) { }

	[CompilerGenerated]
	private void set_MNetherMapFloorPrevIds(Int64[] value) { }

	[CompilerGenerated]
	private void set_NextFloorModelList(List<NetherFloorModel> value) { }

	[CompilerGenerated]
	private void set_PrevFloorModelList(List<NetherFloorModel> value) { }

	[CompilerGenerated]
	private void set_RecommendPower(int value) { }

	[CompilerGenerated]
	private void set_SizeType(NetherFloorSizeType value) { }

	public void UnlockFloor() { }

}

