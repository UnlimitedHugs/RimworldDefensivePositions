using Verse;

namespace DefensivePositions {
	public class DefensivePositionManager : MapComponent {
		public enum PawnInteractionType {
			SavedPosition,
			SentToSavedPosition
		}
		
		private const int PawnControllerCheckEveryTicks = GenTicks.TicksPerRealSecond * 2;

		public static DefensivePositionManager Instance { get; private set; }

		private bool advancedModeEnabled;
		public bool AdvancedModeEnabled {
			get { return advancedModeEnabled; }
		}

		public ModSettingsDef SettingsDef { get; private set; }

		private bool modeSwitchScheduled;
		
		private ScheduledReportData scheduledReport;
				

		public DefensivePositionManager() {
			Instance = this;
			LoadSettingsDef();
			EnsureComponentIsActive();
			LongEventHandler.ExecuteWhenFinished(AddCompToColonists);
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref advancedModeEnabled, "advancedModeEnabled", false);
		}

		public override void MapComponentTick() {
			base.MapComponentTick();
			if (Find.TickManager.TicksGame%PawnControllerCheckEveryTicks == 0) {
				AddCompToColonists();
			}
		}

		public override void MapComponentUpdate() {
			base.MapComponentUpdate();
			if (modeSwitchScheduled) {
				advancedModeEnabled = !advancedModeEnabled;
				modeSwitchScheduled = false;
			}
			var s = scheduledReport;
			if (scheduledReport != null) {
				if (s.reportType == PawnInteractionType.SavedPosition) {
					if (advancedModeEnabled) {
						Messages.Message(string.Format("DefPos_msg_advancedSet".Translate(), s.controlIndex + 1, s.numPawnsSavedPosition), MessageSound.Benefit);
					} else {
						Messages.Message(string.Format("DefPos_msg_basicSet".Translate(), s.numPawnsSavedPosition), MessageSound.Benefit);
					}
				} else if (s.reportType == PawnInteractionType.SentToSavedPosition) {
					if (s.numPawnsHadNoTargetPosition > 0) {
						if (s.numPawnsHadTargetPosition == 0) {
							// no pawns had a valid position
							Messages.Message("DefPos_msg_noposition".Translate(), MessageSound.RejectInput);
						} else if (s.numPawnsHadTargetPosition > 0) {
							// some pawns had valid positions
							Messages.Message(string.Format("DefPos_msg_noposition_partial".Translate(), s.noTargetPositionNames), MessageSound.RejectInput);
						}
					}
					
				}
				scheduledReport = null;
			}
		}

		// actual switching will occur on next frame- due to possible multiple calls
		public void ScheduleAdvancedModeToggle() {
			modeSwitchScheduled = true;
		}

		// interactions can have different outcomes when multiple pawns are selected. So se collect the data and report on in on the next frame.
		public void ReportPawnInteraction(PawnInteractionType type, Pawn pawn, bool success, int usedControlIndex) {
			if (scheduledReport == null) {
				scheduledReport = new ScheduledReportData {reportType = type};
			}
			if (type == PawnInteractionType.SavedPosition) {
				scheduledReport.numPawnsSavedPosition++;
			} else if (type == PawnInteractionType.SentToSavedPosition) {
				if (success) {
					scheduledReport.numPawnsHadTargetPosition++;
				} else {
					scheduledReport.numPawnsHadNoTargetPosition++;
					var pawnName = pawn.NameStringShort;
					if (scheduledReport.noTargetPositionNames == null) {
						scheduledReport.noTargetPositionNames += pawnName;
					} else {
						scheduledReport.noTargetPositionNames += ", " + pawnName;
					}
				}
			}
			scheduledReport.controlIndex = usedControlIndex;
		}

		private void LoadSettingsDef() {
			SettingsDef = DefDatabase<ModSettingsDef>.GetNamed("DefensivePositionsSettings", false);
			if (SettingsDef == null) {
				DefensivePositionsUtility.Error("Missing setting def named DefensivePositionsSettings");
				SettingsDef = new ModSettingsDef();
			}
		}

		// This is a sneaky way to ensure the component is active even on maps where the mod was not active at map creation
		private void EnsureComponentIsActive() {
			LongEventHandler.ExecuteWhenFinished(() => {
				var components = Find.Map.components;
				if (components.Any(c => c is DefensivePositionManager)) return;
				Find.Map.components.Add(this);
			});
		}

		private void AddCompToColonists() {
			var colonists = Find.MapPawns.FreeColonistsSpawned;
			foreach (var colonist in colonists) {
				if(colonist.GetComp<Comp_PawnDefensivePosition>()!=null) continue;
				var comp = new Comp_PawnDefensivePosition {parent = colonist};
				colonist.AllComps.Add(comp);
				comp.Initialize(new CompProperties());
			}
		}

		private class ScheduledReportData {
			public PawnInteractionType reportType;
			public int numPawnsSavedPosition;
			public int numPawnsHadTargetPosition;
			public int numPawnsHadNoTargetPosition;
			public int controlIndex;
			public string noTargetPositionNames;
		}

	}
}