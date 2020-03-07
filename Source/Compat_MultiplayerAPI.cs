using System;
using Multiplayer.API;

namespace DefensivePositions {
	/// <summary>
	/// Compatibility layer for the Multiplayer mod API
	/// Synchronizes key calls to ensure that defensive positions and squads are consistent for all players.
	/// </summary>
	public static class Compat_MultiplayerAPI {
		public static void Initialize() {
			try {
				if (!MP.enabled) return;
				const string expectedAPIVersion = "0.1";
				if (!MP.API.Equals(expectedAPIVersion)) {
					throw new Exception($"MP API version mismatch. This mod is designed to work with MPAPI version {expectedAPIVersion}");
				}
			
				// register synchronized methods
				MP.RegisterSyncMethod(typeof(DefensivePositionsManager), nameof(DefensivePositionsManager.ToggleAdvancedMode));
				MP.RegisterSyncMethod(typeof(PawnSavedPositionHandler), nameof(PawnSavedPositionHandler.SetDefensivePosition));
				MP.RegisterSyncMethod(typeof(PawnSavedPositionHandler), nameof(PawnSavedPositionHandler.DiscardSavedPosition));
				MP.RegisterSyncMethod(typeof(PawnSquadHandler), nameof(PawnSquadHandler.ReassignSquadMembers));
				MP.RegisterSyncMethod(typeof(PawnSquadHandler), nameof(PawnSquadHandler.ClearSquad));

				// register instance resolvers
				MP.RegisterSyncWorker<DefensivePositionsManager>(ManagerSyncer, typeof(DefensivePositionsManager));
				MP.RegisterSyncWorker<PawnSavedPositionHandler>(PawnHandlerSyncer, typeof(PawnSavedPositionHandler));
				MP.RegisterSyncWorker<PawnSquadHandler>(SquadHandlerSyncer, typeof(PawnSquadHandler));

				DefensivePositionsManager.Instance.Logger.Message("Applied Multiplayer API compatibility layer");
			} catch (Exception e) {
				DefensivePositionsManager.Instance.Logger.Error("Failed to apply Multiplayer API compatibility layer: "+e);
			}
		}

		private static void ManagerSyncer(SyncWorker sync, ref DefensivePositionsManager inst) {
			if (!sync.isWriting) inst = DefensivePositionsManager.Instance;
		}

		private static void PawnHandlerSyncer(SyncWorker sync, ref PawnSavedPositionHandler inst) {
			var ownerPawn = inst?.Owner;
			sync.Bind(ref ownerPawn);
			if (!sync.isWriting && ownerPawn != null) inst = DefensivePositionsManager.Instance.GetHandlerForPawn(ownerPawn);
		}

		private static void SquadHandlerSyncer(SyncWorker sync, ref PawnSquadHandler inst) {
			if (!sync.isWriting) inst = DefensivePositionsManager.Instance.squadHandler;
		}
	}
}