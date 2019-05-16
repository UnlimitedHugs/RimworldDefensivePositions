using UnofficialMultiplayerAPI;

namespace DefensivePositions {
	/// <summary>
	/// Compatibility layer for Pecius' Unofficial Multiplayer API
	/// Synchronizes key calls to ensure that defensive positions and squads are consistent for all players.
	/// This class contains only syncers for instance resolution. Synchronized methods are annotated with SyncMethodAttribute.
	/// </summary>
	public static class Compat_MultiplayerAPI {
		[Syncer(shouldConstruct = false)]
		private static void ManagerSyncer(SyncWorker sync, ref DefensivePositionsManager inst) {
			if(!sync.isWriting) inst = DefensivePositionsManager.Instance;
		}

		[Syncer(shouldConstruct = false)]
		private static bool PawnHandlerSyncer(SyncWorker sync, ref PawnSavedPositionHandler inst) {
			var ownerPawn = inst?.Owner;
			sync.Bind(ref ownerPawn);
			if (!sync.isWriting && ownerPawn != null) inst = DefensivePositionsManager.Instance.GetHandlerForPawn(ownerPawn);
			return ownerPawn != null;
		}

		[Syncer(shouldConstruct = false)]
		private static void SquadHandlerSyncer(SyncWorker sync, ref PawnSquadHandler inst) {
			if(!sync.isWriting) inst = DefensivePositionsManager.Instance.squadHandler;
		}
	}
}