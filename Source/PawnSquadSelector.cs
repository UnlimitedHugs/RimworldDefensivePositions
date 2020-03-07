using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld.Planet;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Handles selecting and focusing squad members when a squad is activated
	/// </summary>
	public class PawnSquadSelector {
		private PawnSquad lastActivatedSquad;
		private int repeatedSquadActivations;

		public bool TryActivateSquad(PawnSquad squad) {
			if(!HugsLibUtility.ShiftIsHeld) Find.Selector.ClearSelection();
			var members = squad?.ValidMembers.ToArray();
			if (members != null && members.Length > 0) {
				ProcessRepeatedActivation(squad);
				var potentialFocusTargets = EnumerateCameraFocusTargets(members).ToArray();
				// all squad members may have been despawned, and not found in any caravan. If so, report squad as unassigned
				if (potentialFocusTargets.Length > 0) {
					// loop back to fist target after reaching the end
					var currentActivationTarget = potentialFocusTargets[repeatedSquadActivations % potentialFocusTargets.Length];
					CameraJumper.TryJump(currentActivationTarget);
					if (currentActivationTarget.HasWorldObject) {
						// current target is a caravan
						Find.WorldSelector.ClearSelection();
						Find.WorldSelector.Select(currentActivationTarget.WorldObject);
					} else {
						// current target must be a map location
						var currentMap = currentActivationTarget.Map;
						if (currentMap != null) {
							var squadMembersOnCurrentMap = members.Where(t => t.Map == currentMap);
							foreach (var thing in squadMembersOnCurrentMap) {
								Find.Selector.Select(thing);
							}
						}
					}
					return true;
				}
			}
			return false;
		}

		private void ProcessRepeatedActivation(PawnSquad squad) {
			var isRepeatedActivation = squad == lastActivatedSquad;
			if (isRepeatedActivation) {
				repeatedSquadActivations++;
			} else {
				repeatedSquadActivations = 0;
			}
			lastActivatedSquad = squad;
		}

		private IEnumerable<GlobalTargetInfo> EnumerateCameraFocusTargets(IEnumerable<Thing> things) {
			var sameClusterDistance = DefensivePositionsManager.Instance.SameGroupDistanceSetting.Value;
			var caravans = EnumerateCaravansWithPawns(things.OfType<Pawn>()).ToArray();
			var spawnedThings = things.Where(t => t != null && t.Spawned); // spawned things are not part of caravans
			var spawnedThingsGroupedByMap = spawnedThings.GroupBy(t => t.Map);
			// return centers of clustered pawns first
			foreach (var group in spawnedThingsGroupedByMap) {
				var mapGroupThingPositions = group.Select(t => t.Position);
				foreach (var clusterCenterPos in EnumerateClusteredPositionCenters(mapGroupThingPositions, sameClusterDistance)) {
					yield return new GlobalTargetInfo(clusterCenterPos, group.Key);
				}
			}
			// return caravan positions second
			foreach (var caravan in caravans) {
				yield return new GlobalTargetInfo(caravan);
			}
		}

		private IEnumerable<IntVec3> EnumerateClusteredPositionCenters(IEnumerable<IntVec3> positions, float maxClusterMemberDistance) {
			// for simplicity, the first encountered position is used as base when testing other positions for membership in the same cluster
			var positionsPool = positions.ToList();
			var currentCluster = new List<IntVec3>();
			void PrimeCluster() {
				// try to preserve ordering by always starting with the first pool element
				currentCluster.Add(positionsPool[0]);
				positionsPool.RemoveAt(0);
			}
			bool PositionIsWithinCurrentCluster(IntVec3 pos) => currentCluster[0].InHorDistOf(pos, maxClusterMemberDistance);
			while (positionsPool.Count > 0) {
				PrimeCluster();
				for (var i = positionsPool.Count - 1; i >= 0; i--) {
					if (PositionIsWithinCurrentCluster(positionsPool[i])) {
						currentCluster.Add(positionsPool[i]);
						positionsPool.RemoveAt(i);
					}
				}
				yield return TryGetAveragePosition(currentCluster);
				currentCluster.Clear();
			}
		}

		private IEnumerable<Caravan> EnumerateCaravansWithPawns(IEnumerable<Pawn> pawns) {
			var caravans = Find.WorldObjects.Caravans;
			var searchedPawnSed = pawns.ToHashSet();
			foreach (var caravan in caravans) {
				if(!caravan.IsPlayerControlled) continue;
				var containsAnySearchedPawn = false;
				foreach (var pawn in caravan.PawnsListForReading) {
					if (searchedPawnSed.Contains(pawn)) {
						containsAnySearchedPawn = true;
						break;
					}
				}
				if (containsAnySearchedPawn) {
					yield return caravan;
				}
			}
		}

		private IntVec3 TryGetAveragePosition(List<IntVec3> positions) {
			if (positions.Count > 0) {
				var sum = IntVec3.Zero;
				for (var i = 0; i < positions.Count; i++) {
					sum += positions[i];
				}
				return new IntVec3(sum.x / positions.Count, 0, sum.z / positions.Count);
			}
			return IntVec3.Invalid;
		}
	}
}