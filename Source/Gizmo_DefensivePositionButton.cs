using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DefensivePositions {
	/// <summary>
	/// The basic, single-slot, version of the defensive position button.
	/// </summary>
	public class Gizmo_DefensivePositionButton : Command_Action {
		public bool hasHighPriorityIcon;
		public IDefensivePositionGizmoHandler interactionHandler;
		private List<IDefensivePositionGizmoHandler> groupedHandlers;

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions {
			get { return interactionHandler.GetGizmoContextMenuOptions(0, false); }
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth) {
			var result = base.GizmoOnGUI(topLeft, maxWidth);
			if (result.State == GizmoState.Mouseover) {
				groupedHandlers?.ForEach(h => h.OnBasicGizmoHover());
				interactionHandler.OnBasicGizmoHover();
			}
			return result;
		}

		public override void ProcessInput(Event ev) {
			CurActivateSound?.PlayOneShotOnCamera();
			interactionHandler.OnBasicGizmoAction();
		}

		public override bool GroupsWith(Gizmo other) {
			return other is Gizmo_DefensivePositionButton a && Label == a.Label;
		}

		public override void MergeWith(Gizmo other) {
			base.MergeWith(other);
			if (other is Gizmo_DefensivePositionButton a) {
				if (groupedHandlers == null) groupedHandlers = new List<IDefensivePositionGizmoHandler>();
				groupedHandlers.Add(a.interactionHandler);
				if (a.hasHighPriorityIcon) icon = a.icon;
			}
		}
	}
}