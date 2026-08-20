namespace Project.Nether;

public static class NetherFloorMasterResolver
{
	[CompilerGenerated]
	private sealed class <>c
	{
		public static readonly <>c <>9; //Field offset: 0x0
		public static Func<MNetherFloorEvents, Int64, Boolean> <>9__1_0; //Field offset: 0x8
		public static Func<MNetherFloorEvents, Int64, Boolean> <>9__1_1; //Field offset: 0x10
		public static Func<MNetherFloorShops, Int64, Boolean> <>9__3_0; //Field offset: 0x18
		public static Func<MNetherFloorShops, Int64, Boolean> <>9__3_1; //Field offset: 0x20

		private static <>c() { }

		public <>c() { }

		internal bool <GetMNetherFloorEvents>b__1_0(MNetherFloorEvents masterData, long id) { }

		internal bool <GetMNetherFloorEvents>b__1_1(MNetherFloorEvents masterData, long mId) { }

		internal bool <GetMNetherFloorShops>b__3_0(MNetherFloorShops entity, long id) { }

		internal bool <GetMNetherFloorShops>b__3_1(MNetherFloorShops entity, long mId) { }

	}


	public static MNetherFloorEvents GetMNetherFloorEvents(long mNetherMapFloorId, long extendId) { }

	public static MNetherFloorEvents GetMNetherFloorEvents(MasterDataStore masterDataStore, long mNetherMapFloorId, long extendId) { }

	public static MNetherFloorShops GetMNetherFloorShops(long mNetherMapFloorId, long extendId) { }

	public static MNetherFloorShops GetMNetherFloorShops(MasterDataStore masterDataStore, long mNetherMapFloorId, long extendId) { }

}

