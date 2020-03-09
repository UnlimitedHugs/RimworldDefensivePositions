using System.Collections.Generic;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Allows pawns to be assigned to persistent squads, the members of which can be selected via hotkey press.
	/// </summary>
	public class PawnSquadHandler {
		private const string SquadHotkeyNameBase = "DPSquad";
		private const int NumSquadHotkeys = 9;

		private readonly List<KeyValuePair<KeyBindingDef, int>> squadKeys = new List<KeyValuePair<KeyBindingDef, int>>();
		private readonly PawnSquadSelector squadSelector = new PawnSquadSelector();

		internal static bool ViewingWorldMap {
			get { return WorldRendererUtility.WorldRenderedNow; }
		}

		public PawnSquadHandler() {
			PrepareSquadHotkeys();
		}

		public void OnGUI() {
			if(Current.ProgramState != ProgramState.Playing || Event.current.type != EventType.KeyDown) return;
			PollSquadHotkeys(Event.current);
		}

		private void PrepareSquadHotkeys() {
			for (int i = 1; i < NumSquadHotkeys+1; i++) {
				var hotkeyDef = DefDatabase<KeyBindingDef>.GetNamedSilentFail(SquadHotkeyNameBase + i);
				if(hotkeyDef == null) continue;
				squadKeys.Add(new KeyValuePair<KeyBindingDef, int>(hotkeyDef, i));
			}
		}

		private void PollSquadHotkeys(Event evt) {
			var pressedKey = evt.keyCode;
			if(pressedKey == KeyCode.None) return;
			for (int i = 0; i < squadKeys.Count; i++) {
				var key = squadKeys[i].Key;
				if (KeyPrefs.KeyPrefsData.keyPrefs.TryGetValue(key, out KeyBindingData binding) 
					&& (pressedKey == binding.keyBindingA || pressedKey == binding.keyBindingB)) {
					ProcessSquadCommand(squadKeys[i].Value);
					evt.Use();
					return;
				}
			}
		}

		private void ProcessSquadCommand(int squadNumber) {
			var assignMode = HugsLibUtility.ControlIsHeld;
			var squad = TryFindSquad(squadNumber);
			if (assignMode) {
				// Control is held, assign pawns to squad
				var newMembers = new List<Thing>();
				if (ViewingWorldMap) {
					newMembers.AddRange(EnumeratePlayerPawnsInSelectedCaravans());
				} else {
					newMembers.AddRange(EnumerateSelectedPlayerThingsOnCurrentMap());
				}
				if (newMembers.Count > 0) {
					SortPawnsAndThingsByColonistBarPosition(newMembers);
					ReassignSquadMembers(squadNumber, newMembers);
				} else {
					ClearSquad(squadNumber);
				}
			} else {
				// handle selecting and focusing of squad
				var anySelected = squadSelector.TryActivateSquad(squad);
				if (!anySelected) {
					Messages.Message("DefPos_msg_squadEmpty".Translate(squadNumber), MessageTypeDefOf.RejectInput);
				}
			}
		}

		private PawnSquad TryFindSquad(int squadNumber) {
			var squadsList = DefensivePositionsManager.Instance.SquadData;
			for (var i = 0; i < squadsList.Count; i++) {
				if (squadsList[i].SquadId == squadNumber) {
					return squadsList[i];
				}
			}
			return null;
		}

		private static IEnumerable<Pawn> EnumeratePlayerPawnsInSelectedCaravans() {
			foreach (var obj in Find.WorldSelector.SelectedObjects) {
				if (obj is Caravan car && car.Faction != null && car.Faction.IsPlayer && car.pawns != null) {
					foreach (var pawn in car.pawns) {
						if (pawn?.Faction != null && pawn.Faction.IsPlayer) {
							yield return pawn;
						}
					}
				}
			}
		}

		private static IEnumerable<Thing> EnumerateSelectedPlayerThingsOnCurrentMap() {
			foreach (var obj in Find.Selector.SelectedObjects) {
				if (obj is Thing thing && thing.Faction != null && thing.Faction.IsPlayer && (thing is Pawn || thing is Building)) {
					yield return thing;
				}
			}
		}

		private void SortPawnsAndThingsByColonistBarPosition(List<Thing> things) {
			// put pawns in order of appearance on the bar, followed by buildings
			var colonistBarIndexes = GetPawnIndexesInColonistBar();
			int GetBarIndexOrFallback(Thing t) {
				return t is Pawn p && colonistBarIndexes.TryGetValue(p, out int thingIndexInBar) ? thingIndexInBar : 9999;
			}
			things.Sort((a, b) => GetBarIndexOrFallback(a).CompareTo(GetBarIndexOrFallback(b)));
		}

		private static Dictionary<Pawn, int> GetPawnIndexesInColonistBar() {
			var colonistBarIndexes = new Dictionary<Pawn, int>();
			var currentBarIndex = 0;
			foreach (var barEntry in Find.ColonistBar.Entries) {
				if (barEntry.pawn != null) {
					colonistBarIndexes.Add(barEntry.pawn, currentBarIndex);
					currentBarIndex++;
				}
			}
			return colonistBarIndexes;
		}

		internal void ReassignSquadMembers(int squadNumber, List<Thing> members) {
			var squad = TryFindSquad(squadNumber);
			if (squad == null) {
				squad = new PawnSquad {SquadId = squadNumber};
				DefensivePositionsManager.Instance.SquadData.Add(squad);
			}
			squad.AssignMembers(members);
			Messages.Message("DefPos_msg_squadAssigned".Translate(members.Count, squadNumber), MessageTypeDefOf.TaskCompletion);
		}

		internal void ClearSquad(int squadNumber) {
			if (DefensivePositionsManager.Instance.SquadData.Remove(TryFindSquad(squadNumber))) {
				Messages.Message("DefPos_msg_squadCleared".Translate(squadNumber), MessageTypeDefOf.TaskCompletion);
			}
		}
	}
}