using System.Collections.Generic;
using Verse;

namespace DefensivePositions {
	/// <summary>
	/// The basic defensive position button.
	/// </summary>
	public class Command_DefensivePositionAction : Command_Action {
		public IEnumerable<FloatMenuOption> contextMenuProvider;
		public bool hasHighPriorityIcon;

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions {
			get { return contextMenuProvider; }
		}

		public override bool GroupsWith(Gizmo other) {
			return other is Command_DefensivePositionAction a && Label == a.Label;
		}

		public override void MergeWith(Gizmo other) {
			base.MergeWith(other);
			if (other is Command_DefensivePositionAction a && a.hasHighPriorityIcon) icon = a.icon;
		}
	}
}