namespace Project.Nether.FloorSelection;

public sealed class NetherTreasureFloorEventFlow : NetherFloorEventFlowBase
{
	[CompilerGenerated]
	private sealed class <>c
	{
		public static readonly <>c <>9; //Field offset: 0x0
		public static Func<NetherTreasureFloorEventFlow, Boolean> <>9__5_1; //Field offset: 0x8

		private static <>c() { }

		public <>c() { }

		internal bool <ExecuteAsyncImpl>b__5_1(NetherTreasureFloorEventFlow self) { }

	}

	[CompilerGenerated]
	private sealed class <>c__DisplayClass5_0
	{
		public NetherTreasureFloorEventFlow <>4__this; //Field offset: 0x10
		public NetherModel netherModel; //Field offset: 0x18
		public CancellationToken ct; //Field offset: 0x20

		public <>c__DisplayClass5_0() { }

		internal void <ExecuteAsyncImpl>b__0(NetherEventResultModel resultModel) { }

	}

	[CompilerGenerated]
	private struct <ExecuteAsyncImpl>d__5 : IAsyncStateMachine
	{
		public int <>1__state; //Field offset: 0x0
		public AsyncUniTaskMethodBuilder<NetherEventResultModel> <>t__builder; //Field offset: 0x8
		public NetherTreasureFloorEventFlow <>4__this; //Field offset: 0x20
		public NetherModel netherModel; //Field offset: 0x28
		public CancellationToken ct; //Field offset: 0x30
		private <>c__DisplayClass5_0 <>8__1; //Field offset: 0x38
		private Awaiter <>u__1; //Field offset: 0x40

		private override void MoveNext() { }

		[DebuggerHidden]
		private override void SetStateMachine(IAsyncStateMachine stateMachine) { }

	}

	[CompilerGenerated]
	private struct <HandleEventConfirmedAsync>d__6 : IAsyncStateMachine
	{
		public int <>1__state; //Field offset: 0x0
		public AsyncUniTaskMethodBuilder <>t__builder; //Field offset: 0x8
		public NetherTreasureFloorEventFlow <>4__this; //Field offset: 0x18
		public NetherEventResultModel resultModel; //Field offset: 0x20
		public NetherModel netherModel; //Field offset: 0x28
		public CancellationToken ct; //Field offset: 0x30
		private Awaiter <>u__1; //Field offset: 0x38

		private override void MoveNext() { }

		[DebuggerHidden]
		private override void SetStateMachine(IAsyncStateMachine stateMachine) { }

	}

	private bool _isProcessingEvent; //Field offset: 0x28
	private NetherEventResultModel _eventResultModel; //Field offset: 0x30
	private NetherTreasurePopupController _treasurePopupController; //Field offset: 0x38

	public NetherTreasureFloorEventFlow() { }

	public virtual bool CanHandle(NetherFloorType type) { }

	[AsyncStateMachine(typeof(<ExecuteAsyncImpl>d__5))]
	protected virtual UniTask<NetherEventResultModel> ExecuteAsyncImpl(NetherModel netherModel, CancellationToken ct) { }

	[AsyncStateMachine(typeof(<HandleEventConfirmedAsync>d__6))]
	private UniTask HandleEventConfirmedAsync(NetherModel netherModel, NetherEventResultModel resultModel, CancellationToken ct) { }

	protected virtual UniTask PreloadAsyncImpl(CancellationToken ct) { }

}

