using RimWorld;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// Interactions with the control can have different outcomes when multiple pawns are selected. 
	/// This class collects the data and reports on it on the next frame.
	/// </summary>
	public class ScheduledReportManager {
		public enum ReportType {
			SavedPosition,
			SentToSavedPosition,
			ClearedPosition
		}

		private class ScheduledReport {
			public ReportType reportType;
			public int numPawnsSavedPosition;
			public int numPawnsHadTargetPosition;
			public int numPawnsHadNoTargetPosition;
			public int controlIndex;
			public string noTargetPositionNames;
		}

		private ScheduledReport report;

		public void Update() {
			if (report == null) return;
			if (report.reportType == ReportType.SavedPosition) {
				if (DefensivePositionsManager.Instance.AdvancedModeEnabled) {
					Messages.Message(string.Format("DefPos_msg_advancedSet".Translate(), report.controlIndex + 1, report.numPawnsSavedPosition), MessageTypeDefOf.TaskCompletion);
				} else {
					Messages.Message(string.Format("DefPos_msg_basicSet".Translate(), report.numPawnsSavedPosition), MessageTypeDefOf.TaskCompletion);
				}
			} else if (report.reportType == ReportType.SentToSavedPosition) {
				if (report.numPawnsHadNoTargetPosition > 0) {
					if (report.numPawnsHadTargetPosition == 0) {
						// no pawns had a valid position
						Messages.Message("DefPos_msg_noposition".Translate(), MessageTypeDefOf.RejectInput);
					} else if (report.numPawnsHadTargetPosition > 0) {
						// some pawns had valid positions
						Messages.Message(string.Format("DefPos_msg_noposition_partial".Translate(), report.noTargetPositionNames), MessageTypeDefOf.SilentInput);
					}
				}
			} else if (report.reportType == ReportType.ClearedPosition) {
				if (report.numPawnsHadTargetPosition == 0) {
					Messages.Message("DefPos_msg_clearFailed".Translate(), MessageTypeDefOf.RejectInput);
				} else {
					if (DefensivePositionsManager.Instance.AdvancedModeEnabled) {
						Messages.Message("DefPos_msg_advancedCleared".Translate(report.controlIndex + 1, report.numPawnsHadTargetPosition), MessageTypeDefOf.TaskCompletion);
					} else {
						Messages.Message("DefPos_msg_basicCleared".Translate(report.numPawnsHadTargetPosition), MessageTypeDefOf.TaskCompletion);
					}
				}
			}
			report = null;
		}

		public void ReportPawnInteraction(ReportType type, Pawn pawn, bool success, int usedControlIndex) {
			if (report == null) {
				report = new ScheduledReport { reportType = type };
			}
			switch (type) {
				case ReportType.SavedPosition:
					report.numPawnsSavedPosition++;
					break;
				case ReportType.SentToSavedPosition:
					if (success) {
						report.numPawnsHadTargetPosition++;
					} else {
						report.numPawnsHadNoTargetPosition++;
						var pawnName = pawn.Name.ToStringShort;
						if (report.noTargetPositionNames == null) {
							report.noTargetPositionNames += pawnName;
						} else {
							report.noTargetPositionNames += ", " + pawnName;
						}
					}
					break;
				case ReportType.ClearedPosition:
					if (success) {
						report.numPawnsHadTargetPosition++;
					}
					break;
			}
			report.controlIndex = usedControlIndex;
		}

	}
}