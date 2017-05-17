using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DefensivePositions {
	/**
	 * Allows pawns to be assigned to persistent squads, the members of which can be selected via hotkey press.
	 */
	public class PawnSquadHandler {
		private const string SquadHotkeyNameBase = "DPSquad";
		private const int NumSquadHotkeys = 9;

		private readonly List<KeyValuePair<KeyBindingDef, int>> squadKeys = new List<KeyValuePair<KeyBindingDef, int>>();
		
		public PawnSquadHandler() {
			PrepareSquadHotkeys();
		}

		public void OnGUI() {
			if(Current.ProgramState != ProgramState.Playing || Event.current.type != EventType.KeyDown) return;
			PollSquadHotkeys(Event.current.keyCode);
		}

		private void PrepareSquadHotkeys() {
			for (int i = 1; i < NumSquadHotkeys+1; i++) {
				var hotkeyDef = DefDatabase<KeyBindingDef>.GetNamedSilentFail(SquadHotkeyNameBase + i);
				if(hotkeyDef == null) continue;
				squadKeys.Add(new KeyValuePair<KeyBindingDef, int>(hotkeyDef, i));
			}
		}

		private void PollSquadHotkeys(KeyCode pressedKey) {
			if(pressedKey == KeyCode.None) return;
			for (int i = 0; i < squadKeys.Count; i++) {
				var key = squadKeys[i].Key;
				KeyBindingData binding;
				if (KeyPrefs.KeyPrefsData.keyPrefs.TryGetValue(key, out binding) && (pressedKey == binding.keyBindingA || pressedKey == binding.keyBindingB)) {
					ProcessSquadCommand(squadKeys[i].Value);
					return;
				}
			}
		}

		private void ProcessSquadCommand(int squadNumber) {
			var assignMode = HugsLibUtility.ControlIsHeld;
			var pawnSquads = DefensivePositionsManager.Instance.SquadData;
			var squad = pawnSquads.Find(s => s.squadId == squadNumber);
			if (assignMode) {
				// Control is held, assign pawns to squad
				var idList = new List<int>();
				foreach (var obj in Find.Selector.SelectedObjects) {
					var pawn = obj as Pawn;
					if(pawn == null || pawn.Faction == null || !pawn.Faction.IsPlayer) continue;
					idList.Add(pawn.thingIDNumber);
				}
				if (idList.Count > 0) {
					// reassign squad with selected pawns
					Messages.Message("DefPos_msg_squadAssigned".Translate(idList.Count, squadNumber), MessageSound.Benefit);
					if (squad == null) {
						squad = new PawnSquad {squadId = squadNumber};
						pawnSquads.Add(squad);
					}
					squad.pawnIds = idList;
				} else {
					// no pawns selected, clear squad
					Messages.Message("DefPos_msg_squadCleared".Translate(squadNumber), MessageSound.Benefit);
					if (squad != null) pawnSquads.Remove(squad);
				}
			} else {
				// Select pawns that belong to squad
				var selectionBeforeClear = Find.Selector.SelectedObjects.ToList();
				if(!HugsLibUtility.ShiftIsHeld) Find.Selector.ClearSelection();
				List<Pawn> matchingPawnsOnMaps = null;
				Caravan matchingCaravan = null;
				if (squad != null && squad.pawnIds.Count > 0) {
					matchingPawnsOnMaps = GetLivePawnsOnAllMapsById(squad.pawnIds);
					if (matchingPawnsOnMaps.Count == 0) {
						matchingCaravan = TryGetFirstCaravanWithPawnsById(squad.pawnIds);
					}
				}
				if (matchingPawnsOnMaps!=null && matchingPawnsOnMaps.Count>0) {
					var pawns = SelectOnlyPawnsOnSameMap(matchingPawnsOnMaps);
					// focus view on squad if repeat squad key press OR if not currently viewing the map
					if (Find.VisibleMap != pawns[0].Map || InWorldView() || PawnsAlreadyMatchSelection(pawns, selectionBeforeClear)) {	
						TryEscapeWorldView();
						TryFocusPawnGroupCenter(pawns);
					}
					// select pawns on map, switch map if necessary
					foreach (var pawn in pawns) {
						Find.Selector.Select(pawn);
					}
				} else if (matchingCaravan != null) {
					// select caravan with pawns
					CameraJumper.TryJumpAndSelect(matchingCaravan);
				} else {
					Messages.Message("DefPos_msg_squadEmpty".Translate(squadNumber), MessageSound.RejectInput);
				}
			}
		}

		private List<Pawn> GetLivePawnsOnAllMapsById(List<int> pawnIds) {
			var results = new List<Pawn>();
			for (int i = 0; i < Current.Game.Maps.Count; i++) {
				var mapPawns = Current.Game.Maps[i].mapPawns.AllPawnsSpawned;
				for (int j = 0; j < mapPawns.Count; j++) {
					var pawn = mapPawns[j];
					if (pawn.Dead) continue;
					if (pawnIds.Contains(pawn.thingIDNumber)) {
						results.Add(pawn);
					} 
				}
			}
			return results;
		}

		// filter the list by dropping all pawns that are on a different map than the first one
		private List<Pawn> SelectOnlyPawnsOnSameMap(List<Pawn> pawns) {
			Map firstMap = null;
			var results = new List<Pawn>();
			foreach (var pawn in pawns) {
				if (firstMap == null) firstMap = pawn.Map;
				if(pawn.Map != firstMap) continue;
				results.Add(pawn);
			}
			return results;
		}

		private Caravan TryGetFirstCaravanWithPawnsById(List<int> pawnIds) {
			var caravans = Find.WorldObjects.Caravans;
			foreach (var caravan in caravans) {
				if(!caravan.IsPlayerControlled) continue;
				foreach (var pawn in caravan.PawnsListForReading) {
					if (pawnIds.Contains(pawn.thingIDNumber)) return caravan;
				}
			}
			return null;
		}

		// switches to the map the pawns are on and moves the camera to the center point of the group
		private void TryFocusPawnGroupCenter(List<Pawn> pawns) {
			if (pawns.Count == 0) return;
			var sum = IntVec3.Zero;
			foreach (var pawn in pawns) {
				sum += pawn.Position;
			}
			var average = new IntVec3(sum.x/pawns.Count, 0, sum.z/pawns.Count);
			CameraJumper.TryJump(new GlobalTargetInfo(average, pawns[0].Map));
		}

		private bool InWorldView() {
			return Find.MainTabsRoot.OpenTab == MainButtonDefOf.World;
		}

		private void TryEscapeWorldView() {
			if(!InWorldView()) return;
			Find.MainTabsRoot.EscapeCurrentTab();
		}

		private bool PawnsAlreadyMatchSelection(List<Pawn> pawns, List<object> selection) {
			foreach (var pawn in pawns) {
				if (!selection.Contains(pawn)) return false;
			}
			return selection.Count == pawns.Count;
		}
	}
}