using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Handles selecting and focusing squad members when a squad is activated.
	/// Repeated activations of the same squad will cycle through all "interest points" of this squad-
	/// including clusters of pawns on each map, as well as caravans on the world map
	/// </summary>
	public class PawnSquadSelector {
		// how long it takes for an additional press of the squad hotkey to count as the first in a sequence
		private const float ActivationSequenceExpireTime = 3f;

		private int lastActivatedSquadId;
		private float lastActivationTime;
		/* 
		Stores the map index where cycling began. Null if starting on the world map.
		Must be stored to preserve order when jumping between interest points.
		 */
		private int? lastStartingMapIndex;
		/* 
		Allows to sort interest points by increasing distance from camera. Stored to preserve order when jumping between interest points.
		This position won't carry any meaning when jumping to other maps, but we don't care about minimizing jump distance on those, either. 
		*/
		private IntVec3 lastStartingMapCameraPosition;
		private int interestPointIndex;

		private static Map CurrentlyViewedMap {
			get { return PawnSquadHandler.ViewingWorldMap ? null : Find.CurrentMap; }
		}

		public bool TryActivateSquad(PawnSquad squad) {
			var members = squad?.ValidMembers.ToArray();
			if (members != null && members.Length > 0) {
				var isFirstActivationInSequence = CheckForFirstActivation(squad);
				var potentialInterestPoints = EnumerateInterestPoints(members, lastStartingMapCameraPosition).ToList();
				SortInterestPointsByStartingMap(potentialInterestPoints, lastStartingMapIndex);
				// if all squad members have been despawned, and not found in any caravan, report squad as unassigned
				if (potentialInterestPoints.Count > 0) {
					// loop back to fist target after reaching the end
					var currentActivationTarget = potentialInterestPoints[interestPointIndex % potentialInterestPoints.Count];
					var shouldJumpToTarget = currentActivationTarget.IsWorldTarget
											|| !isFirstActivationInSequence
											|| CurrentlyViewedMap != currentActivationTarget.Map;
					if (shouldJumpToTarget) {
						CameraJumper.TryJump(currentActivationTarget);
						interestPointIndex++;
					}
					TrySelectActivationTargetSquadMembers(members, currentActivationTarget);
					return true;
				}
			}
			return false;
		}

		private bool CheckForFirstActivation(PawnSquad activatedSquad) {
			bool isFirstActivation;
			if (lastActivatedSquadId != activatedSquad.SquadId || Time.unscaledTime > lastActivationTime + ActivationSequenceExpireTime) {
				lastActivatedSquadId = activatedSquad.SquadId;
				interestPointIndex = 0;
				lastStartingMapIndex = CurrentlyViewedMap?.Index;
				lastStartingMapCameraPosition = Find.CurrentMap?.rememberedCameraPos?.rootPos.ToIntVec3() ?? IntVec3.Zero;
				isFirstActivation = true;
			} else {
				isFirstActivation = false;
			}
			lastActivationTime = Time.unscaledTime;
			return isFirstActivation;
		}

		private static IEnumerable<GlobalTargetInfo> EnumerateInterestPoints(IEnumerable<Thing> things, IntVec3 startingPosition) {
			var sameClusterDistance = DefensivePositionsManager.Instance.SameGroupDistanceSetting.Value;
			var caravans = EnumerateCaravansWithPawns(things).ToArray();
			var spawnedThings = things.Where(t => t != null && t.Spawned); // spawned things are not part of caravans
			var spawnedThingsGroupedByMap = spawnedThings.GroupBy(t => t.Map);
			// return centers of clustered pawns first
			foreach (var group in spawnedThingsGroupedByMap) {
				var map = group.Key;
				var thingPositions = group.Select(t => t.Position);
				var clusterPositions = EnumerateClusteredPositionCenters(thingPositions, sameClusterDistance, startingPosition);
				foreach (var clusterCenterPos in clusterPositions) {
					yield return new GlobalTargetInfo(clusterCenterPos, map);
				}
			}
			// return caravan positions second
			foreach (var caravan in caravans) {
				yield return new GlobalTargetInfo(caravan);
			}
		}

		/// <summary>
		/// Groups given positions into clusters by distance from each other
		/// </summary>
		/// <param name="positions">Positions to sort into clusters</param>
		/// <param name="maxClusterMemberDistance">Maximum distance between positions to be considered part of the same cluster</param>
		/// <param name="startingPosition">Clusters are returned in order of increasing distance from starting position or previous cluster</param>
		/// <returns>Average of all positions part of each cluster, rounded to the nearest cell</returns>
		private static IEnumerable<IntVec3> EnumerateClusteredPositionCenters(
			IEnumerable<IntVec3> positions, float maxClusterMemberDistance, IntVec3 startingPosition) {
			var positionsPool = positions.ToList();
			var currentCluster = new List<IntVec3>();
			var currentClusterBasePosition = startingPosition;
			IntVec3 TakeClosestPositionFromPool(IntVec3 basePos) {
				var closestIndex = GetIndexOfClosestPosition(basePos, positionsPool);
				var closestPos = positionsPool[closestIndex];
				positionsPool.RemoveAt(closestIndex);
				return closestPos;
			}
			void PrimeCluster() {
				var clusterBasePosition = TakeClosestPositionFromPool(currentClusterBasePosition);
				currentCluster.Add(clusterBasePosition);
				currentClusterBasePosition = clusterBasePosition;
			}
			bool PositionIsWithinCurrentCluster(IntVec3 pos) {
				return currentClusterBasePosition.InHorDistOf(pos, maxClusterMemberDistance);
			}
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

		private static void SortInterestPointsByStartingMap(List<GlobalTargetInfo> interestPoints, int? startingMapIndex) {
			// targets on the first activation map should be returned first
			int GetTargetPriority(GlobalTargetInfo t) {
				if (startingMapIndex.HasValue) {
					return t.Map != null && t.Map.Index == startingMapIndex ? 1 : 2;
				} else {
					return t.HasWorldObject ? 1 : 2;
				}
			}
			interestPoints.SortStable((a, b) => GetTargetPriority(a).CompareTo(GetTargetPriority(b)));
		}

		private static IEnumerable<Caravan> EnumerateCaravansWithPawns(IEnumerable<Thing> pawns) {
			var caravans = Find.WorldObjects.Caravans;
			var searchedPawnSet = pawns.ToHashSet();
			foreach (var caravan in caravans) {
				if(!caravan.IsPlayerControlled) continue;
				var containsAnySearchedPawn = false;
				foreach (var pawn in caravan.PawnsListForReading) {
					if (searchedPawnSet.Contains(pawn)) {
						containsAnySearchedPawn = true;
						break;
					}
				}
				if (containsAnySearchedPawn) {
					yield return caravan;
				}
			}
		}

		private static IntVec3 TryGetAveragePosition(List<IntVec3> positions) {
			if (positions.Count > 0) {
				var sum = IntVec3.Zero;
				for (var i = 0; i < positions.Count; i++) {
					sum += positions[i];
				}
				return new IntVec3(sum.x / positions.Count, 0, sum.z / positions.Count);
			}
			return IntVec3.Invalid;
		}

		private static int GetIndexOfClosestPosition(IntVec3 basePos, List<IntVec3> positions) {
			var closestPosIndex = -1;
			float closestPosDistanceSquared = float.MaxValue;
			for (var i = 0; i < positions.Count; i++) {
				var distanceSquared = positions[i].DistanceToSquared(basePos);
				if (distanceSquared < closestPosDistanceSquared) {
					closestPosIndex = i;
					closestPosDistanceSquared = distanceSquared;
				}
			}
			return closestPosIndex;
		}

		private static IEnumerable<Thing> FilterThingsByMap(IEnumerable<Thing> members, Map currentMap) {
			return members.Where(t => t.Map == currentMap);
		}

		private static void TrySelectActivationTargetSquadMembers(IEnumerable<Thing> squadMembers, GlobalTargetInfo activationTarget) {
			var additiveSelection = HugsLibUtility.ShiftIsHeld;
			if (activationTarget.HasWorldObject) {
				// current target is a caravan
				if (!additiveSelection) Find.WorldSelector.ClearSelection();
				Find.WorldSelector.ClearSelection();
				Find.WorldSelector.Select(activationTarget.WorldObject);
			} else {
				// current target must be a map location
				if (!additiveSelection) Find.Selector.ClearSelection();
				var currentMap = activationTarget.Map;
				if (currentMap != null) {
					var squadMembersOnCurrentMap = FilterThingsByMap(squadMembers, currentMap);
					foreach (var thing in squadMembersOnCurrentMap) {
						Find.Selector.Select(thing);
					}
				}
			}
		}
	}
}