using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Contains the data stored within a game save
	/// </summary>
	public class WorldData : WorldComponent {
		public bool advancedModeEnabled;
		public bool AdvancedModeEnabled {
			get { return advancedModeEnabled; }
			set { advancedModeEnabled = value; }
		}

		public int lastAdvancedControlUsed;
		public int LastAdvancedControlUsed {
			get { return lastAdvancedControlUsed; }
			set { lastAdvancedControlUsed = value; }
		}

		public List<PawnSquad> pawnSquads = new List<PawnSquad>();
		public List<PawnSquad> PawnSquads {
			get { return pawnSquads; }
			set { pawnSquads = value; }
		}

		private Dictionary<Pawn, PawnSavedPositionHandler> handlers = new Dictionary<Pawn, PawnSavedPositionHandler>();

		private List<PawnSavedPositionHandler> tempHandlerSavingList;

		public WorldData(World world) : base(world) {
		}

		public PawnSavedPositionHandler GetOrAddPawnHandler(Pawn pawn) {
			var handler = handlers.TryGetValue(pawn);
			if (handler == null) {
				handler = new PawnSavedPositionHandler();
				handler.AssignOwner(pawn);
				handlers.Add(pawn, handler);
			}
			return handler;
		}

		public override void ExposeData() {
			var mode = Scribe.mode;
			Scribe_Values.Look(ref advancedModeEnabled, "advancedModeEnabled");
			Scribe_Values.Look(ref lastAdvancedControlUsed, "lastAdvancedControlUsed");
			if (mode == LoadSaveMode.Saving) {
				// convert to list first- we can get the keys from the handlers at load time
				tempHandlerSavingList = HandlerListFromDictionary(handlers);
				DiscardNonSaveWorthySquads();
			}
			Scribe_Collections.Look(ref tempHandlerSavingList, "savedPositions", LookMode.Deep);
			Scribe_Collections.Look(ref pawnSquads, "pawnSquads", LookMode.Deep);
			if (mode == LoadSaveMode.PostLoadInit) {
				handlers = HandlerListToDictionary(tempHandlerSavingList);
				tempHandlerSavingList = null;
				if (PawnSquads == null) PawnSquads = new List<PawnSquad>();
				LastAdvancedControlUsed = Mathf.Clamp(LastAdvancedControlUsed, 0, PawnSavedPositionHandler.NumAdvancedPositionButtons - 1);
			}
		}

		internal void OnMapDiscarded(Map map) {
			foreach (var handler in handlers.Values) {
				handler.OnMapDiscarded(map);
			}
		}

		private static List<PawnSavedPositionHandler> HandlerListFromDictionary(Dictionary<Pawn, PawnSavedPositionHandler> dict) {
			return dict.Values
				.Where(v => v.ShouldBeSaved)
				.ToList();
		}

		private static Dictionary<Pawn, PawnSavedPositionHandler> HandlerListToDictionary(List<PawnSavedPositionHandler> list) {
			return (list ?? Enumerable.Empty<PawnSavedPositionHandler>())
				.Where(psp => psp?.Owner != null)
				.ToDictionary(psp => psp.Owner, v => v);
		}

		private void DiscardNonSaveWorthySquads() {
			pawnSquads.RemoveAll(s => s == null || !s.ShouldBeSaved);
		}
	}
}