using System.Collections.Generic;
using Verse;

namespace DefensivePositions {
	public class Command_ActionWithExternalContextMenu : Command_Action {
		public IEnumerable<FloatMenuOption> contextMenuProvider;

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions {
			get { return contextMenuProvider; }
		}
	}
}