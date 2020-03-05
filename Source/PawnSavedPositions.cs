using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Base class for <see cref="PawnSavedPositionHandler"/> that takes care of data storage
	/// </summary>
	public abstract class PawnSavedPositions : IExposable {
		protected const int NumStoredPositions = 4;

		public Pawn Owner {
			get { return owner; }
		}

		public bool ShouldBeSaved {
			get { return owner != null && !owner.Destroyed && owner.Faction == Faction.OfPlayer; }
		}

		private Pawn owner;
		private Dictionary<Map, List<IntVec3>> savedPositions = new Dictionary<Map, List<IntVec3>>();
		private List<MapPositionSet> tempPositionSavingList;

		public void ExposeData() {
			Scribe_References.Look(ref owner, "owner");
			var mode = Scribe.mode;
			if (mode == LoadSaveMode.Saving) {
				// convert to list first. Using a nested list as a value in a dictionary is not valid
				tempPositionSavingList = MapPositionSet.ListFromDictionary(savedPositions);
			}
			if (mode == LoadSaveMode.Saving || mode == LoadSaveMode.LoadingVars) {
				// prevent loaded values from being reset during resolve crossrefs phase
				Scribe_Collections.Look(ref tempPositionSavingList, "positions", LookMode.Deep);
			}
			if (mode == LoadSaveMode.PostLoadInit) {
				savedPositions = MapPositionSet.ListToDictionary(tempPositionSavingList);
				tempPositionSavingList = null;
			}
		}

		internal void AssignOwner(Pawn newOwner) {
			owner = newOwner;
		}

		internal void OnMapDiscarded(Map map) {
			savedPositions.Remove(map);
		}

		protected bool HasSavedPosition(int slot) {
			return GetPosition(slot).IsValid;
		}

		protected IntVec3 GetPosition(int slot) {
			CheckOwnerSpawned();
			return savedPositions.TryGetValue(owner.Map, out List<IntVec3> positions)
				? positions[slot]
				: IntVec3.Invalid;
		}

		protected void SetPosition(int slot, IntVec3 position) {
			CheckOwnerSpawned();
			var positions = savedPositions.TryGetValue(owner.Map) ?? ResetSavedPositionsForMap(owner.Map);
			positions[slot] = position;
		}

		protected void DiscardPosition(int slot) {
			SetPosition(slot, IntVec3.Invalid);
		}

		private void CheckOwnerSpawned() {
			if (!owner.Spawned)
				throw new InvalidOperationException(
					$"Cannot access saved positions while owner ({owner.ToStringSafe()}) is not spawned."
				);
		}

		private List<IntVec3> ResetSavedPositionsForMap(Map map) {
			var positions = new List<IntVec3>(Enumerable.Repeat(IntVec3.Invalid, NumStoredPositions));
			savedPositions[map] = positions;
			return positions;
		}

		/// <summary>
		/// A container that allows a position list associated to a map to be properly saved
		/// </summary>
		public class MapPositionSet : IExposable {
			public static List<MapPositionSet> ListFromDictionary(Dictionary<Map, List<IntVec3>> dict) {
				bool MapIsRegistered(Map map) => Find.Maps == null || Find.Maps.Contains(map);

				return new List<MapPositionSet>(dict
					.Where(kv => MapIsRegistered(kv.Key))
					.Select(kv => new MapPositionSet {Map = kv.Key, Positions = kv.Value})
				);
			}

			public static Dictionary<Map, List<IntVec3>> ListToDictionary(List<MapPositionSet> list) {
				var dict = new Dictionary<Map, List<IntVec3>>();
				foreach (var set in list) {
					if (set.Map == null) continue;
					dict.Add(set.Map, set.Positions);
				}
				return dict;
			}

			private Map map;
			public Map Map {
				get { return map; }
				set { map = value; }
			}

			private List<IntVec3> positions;
			public List<IntVec3> Positions {
				get { return positions; }
				set { positions = value; }
			}

			public void ExposeData() {
				Scribe_References.Look(ref map, "map");
				Scribe_Collections.Look(ref positions, "positions", LookMode.Value);
				if (Scribe.mode == LoadSaveMode.PostLoadInit) {
					NormalizeLoadedList();
				}
			}

			private void NormalizeLoadedList() {
				if (positions == null) positions = new List<IntVec3>();
				while (positions.Count > NumStoredPositions) {
					positions.RemoveAt(positions.Count - 1);
				}
				while (positions.Count < NumStoredPositions) {
					positions.Add(IntVec3.Invalid);
				}
			}

			public override string ToString() {
				return $"[{map?.Index.ToStringSafe()} {positions.ListElements()}]";
			}
		}
	}
}