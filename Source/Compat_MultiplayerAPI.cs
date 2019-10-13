using Multiplayer.API;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Compatibility layer for the Multiplayer mod API
	/// Synchronizes key calls to ensure that defensive positions and squads are consistent for all players.
	/// This class contains only syncers for instance resolution. Synchronized methods are annotated with SyncMethodAttribute.
	/// </summary>
	public static class Compat_MultiplayerAPI {
		public static void Initialize() {
			if (!MP.enabled) return;
			const string expectedAPIVersion = "0.1";
			if (!MP.API.Equals(expectedAPIVersion)) {
				DefensivePositionsManager.Instance.Logger.Error($"MP API version mismatch. This mod is designed to work with MPAPI version {expectedAPIVersion}");
				return;
			}
			MP.RegisterAll();
		}

		[SyncWorker(shouldConstruct = false)]
		private static void ManagerSyncer(SyncWorker sync, ref DefensivePositionsManager inst) {
			if (!sync.isWriting) inst = DefensivePositionsManager.Instance;
		}

		[SyncWorker(shouldConstruct = false)]
		private static void PawnHandlerSyncer(SyncWorker sync, ref PawnSavedPositionHandler inst) {
			var ownerPawn = inst?.Owner;
			sync.Bind(ref ownerPawn);
			if (!sync.isWriting && ownerPawn != null) inst = DefensivePositionsManager.Instance.GetHandlerForPawn(ownerPawn);
		}

		[SyncWorker(shouldConstruct = false)]
		private static void SquadHandlerSyncer(SyncWorker sync, ref PawnSquadHandler inst) {
			if (!sync.isWriting) inst = DefensivePositionsManager.Instance.squadHandler;
		}
	}
}