namespace Project.Master;

public sealed class MasterDataStore : IEngineServiceRegister, IAbstractServiceRegister
{
	[CompilerGenerated]
	private sealed class <>c
	{
		public static readonly <>c <>9; //Field offset: 0x0
		public static Func<MethodInfo, Boolean> <>9__7_0; //Field offset: 0x8

		private static <>c() { }

		public <>c() { }

		internal bool <get_SetCacheGenericDefinition>b__7_0(MethodInfo m) { }

	}

	[CompilerGenerated]
	private struct <DownloadFirstAsync>d__8 : IAsyncStateMachine
	{
		public int <>1__state; //Field offset: 0x0
		public AsyncUniTaskMethodBuilder <>t__builder; //Field offset: 0x8
		public MasterDataStore <>4__this; //Field offset: 0x18
		public string masterVersion; //Field offset: 0x20
		public CancellationToken ct; //Field offset: 0x28
		public int diskCacheBytesPerYieldChunk; //Field offset: 0x30
		private FirstDownloadParam<MFirstDownload> <param>5__2; //Field offset: 0x38
		private bool <deserialized>5__3; //Field offset: 0x40
		private Awaiter<Byte[]> <>u__1; //Field offset: 0x48
		private Awaiter <>u__2; //Field offset: 0x60
		private Awaiter<Boolean> <>u__3; //Field offset: 0x70

		private override void MoveNext() { }

		[DebuggerHidden]
		private override void SetStateMachine(IAsyncStateMachine stateMachine) { }

	}

	[CompilerGenerated]
	private struct <GetAsync>d__10 : IAsyncStateMachine
	{
		public int <>1__state; //Field offset: 0x0
		public AsyncUniTaskMethodBuilder<T[]> <>t__builder; //Field offset: 0x0
		public MasterDataStore <>4__this; //Field offset: 0x0
		public CancellationToken ct; //Field offset: 0x0
		private Type <type>5__2; //Field offset: 0x0
		private Awaiter<MasterLoadResult<T>> <>u__1; //Field offset: 0x0

		private override void MoveNext() { }

		[DebuggerHidden]
		private override void SetStateMachine(IAsyncStateMachine stateMachine) { }

	}

	[CompilerGenerated]
	private struct <TryPersistFirstDownloadDiskCacheAsync>d__9 : IAsyncStateMachine
	{
		public int <>1__state; //Field offset: 0x0
		public AsyncUniTaskMethodBuilder <>t__builder; //Field offset: 0x8
		public MasterDataStore <>4__this; //Field offset: 0x18
		public string masterVersion; //Field offset: 0x20
		public CancellationToken ct; //Field offset: 0x28
		public int diskCacheBytesPerYieldChunk; //Field offset: 0x30
		private Awaiter <>u__1; //Field offset: 0x38

		private override void MoveNext() { }

		[DebuggerHidden]
		private override void SetStateMachine(IAsyncStateMachine stateMachine) { }

	}

	private const int SecureLinkExpireSeconds = 600; //Field offset: 0x0
	private const int FirstDownloadTimeoutSeconds = 30; //Field offset: 0x0
	private const string DecryptKey = "abyss"; //Field offset: 0x0
	private static MethodInfo _setCacheGenericDefinition; //Field offset: 0x0
	private DefaultMasterLoader<MFirstDownload> _masterLoader; //Field offset: 0x10
	private readonly Dictionary<Type, IMasterLoadResult> _caches; //Field offset: 0x18

	private static MethodInfo SetCacheGenericDefinition
	{
		private get { } //Length: 445
	}

	public MasterDataStore() { }

	[CompilerGenerated]
	private void <DownloadFirstAsync>b__8_0(Type elementType, object rowsArray, bool useLocal) { }

	[AsyncStateMachine(typeof(<DownloadFirstAsync>d__8))]
	public UniTask DownloadFirstAsync(string masterVersion, CancellationToken ct, int diskCacheBytesPerYieldChunk = 262144) { }

	private static MethodInfo get_SetCacheGenericDefinition() { }

	[AsyncStateMachine(typeof(<GetAsync>d__10`1))]
	public UniTask<T[]> GetAsync(CancellationToken ct) { }

	public T[] GetCache() { }

	[Preserve]
	public void SetCache(T[] rows) { }

	[Preserve]
	public void SetCacheImpl(T[] rows, bool useLocal) { }

	private bool TryGetCache(out T[] rows) { }

	[AsyncStateMachine(typeof(<TryPersistFirstDownloadDiskCacheAsync>d__9))]
	private UniTask TryPersistFirstDownloadDiskCacheAsync(string masterVersion, CancellationToken ct, int diskCacheBytesPerYieldChunk) { }

}

