using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DefensivePositions {
	/// <summary>
	/// A gizmo with 4 separate interactable button areas.
	/// Uses a single atlas texture and requires a set of uv rects to be provided to draw the slot textures.
	/// Slots can be individually switched to alternative uv coordinates using the <see cref="activeIconMask"/>.
	/// </summary>
	public class Gizmo_QuadButtonPanel : Command {
		private const float ContentPadding = 5f;
		private const float GizmoSize = 75f;
		private const float IconSize = 32f;

		private static readonly Color iconBaseColor = new Color(.5f, .5f, .5f, 1f);
		private static readonly Color iconMouseOverAdd = new Color(.1f, .1f, .1f, 0f);

		// must be static because GizmoOnGUI is called only for the first gizmo, but ProcessInput is called for all grouped gizmos
		private static int hoveredControlIndex;

		public Texture2D atlasTexture;
		public Rect[] iconUVsInactive;
		public Rect[] iconUVsActive;
		public byte activeIconMask;
		public IDefensivePositionGizmoHandler interactionHandler;
		private List<IDefensivePositionGizmoHandler> groupedHandlers;

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions {
			get {
				return hoveredControlIndex >= 0
					? interactionHandler.GetGizmoContextMenuOptions(hoveredControlIndex, true)
					: Enumerable.Empty<FloatMenuOption>();
			}
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms _) {
			var gizmoRect = new Rect(topLeft.x, topLeft.y, GizmoSize, GizmoSize);
			var contentRect = gizmoRect.ContractedBy(ContentPadding);
			Widgets.DrawWindowBackground(gizmoRect);
			var state = GizmoState.Clear;
			hoveredControlIndex = -1;

			for (int i = 0; i < 4; i++) {
				Vector2 offset;
				switch (i) {
					case 1:
						offset = new Vector2(IconSize, 0);
						break;
					case 2:
						offset = new Vector2(0, IconSize);
						break;
					case 3:
						offset = new Vector2(IconSize, IconSize);
						break;
					default:
						offset = new Vector2();
						break;
				}
				var iconRect = new Rect(contentRect.x + offset.x, contentRect.y + offset.y,
					Mathf.Floor(contentRect.width / 2f), Mathf.Floor(contentRect.height / 2f));
				var iconColor = iconBaseColor;

				TooltipHandler.TipRegion(iconRect, string.Format(defaultDesc, i + 1));
				MouseoverSounds.DoRegion(iconRect, SoundDefOf.Mouseover_Command);
				if (Mouse.IsOver(iconRect)) {
					Widgets.DrawHighlight(iconRect);
					state = GizmoState.Mouseover;
					iconColor += iconMouseOverAdd;
					hoveredControlIndex = i;
					groupedHandlers?.ForEach(h => h.OnAdvancedGizmoHover(i));
					interactionHandler.OnAdvancedGizmoHover(i);
				}
				if (Widgets.ButtonInvisible(iconRect)) {
					hoveredControlIndex = i;
					switch (Event.current.button) {
						case 0:
							state = GizmoState.Interacted;
							break;
						case 1:
							state = GizmoState.OpenedFloatMenu;
							break;
					}
				}
				var iconUVRect = ((activeIconMask & (1 << i)) != 0 ? iconUVsActive : iconUVsInactive)[i];

				Graphics.DrawTexture(iconRect, atlasTexture, iconUVRect, 0, 0, 0, 0, iconColor);
			}

			DrawHotKeyLabel(gizmoRect);
			if (hotKey != null && hotKey.KeyDownEvent) {
				state = GizmoState.Interacted;
				hoveredControlIndex = -1;
				Event.current.Use();
			}
			DrawGizmoLabel(defaultLabel, gizmoRect);
			return state == GizmoState.Clear ? new GizmoResult(GizmoState.Clear) : new GizmoResult(state, Event.current);
		}

		public override float GetWidth(float maxWidth) {
			return GizmoSize;
		}

		public override void ProcessInput(Event ev) {
			activateSound?.PlayOneShotOnCamera();
			if (hoveredControlIndex >= 0) {
				interactionHandler.OnAdvancedGizmoClick(hoveredControlIndex);
			} else {
				interactionHandler.OnAdvancedGizmoHotkeyDown();
			}
		}

		public override bool GroupsWith(Gizmo other) {
			return other is Gizmo_QuadButtonPanel q && Label == q.Label;
		}

		public override void MergeWith(Gizmo other) {
			base.MergeWith(other);
			// if an icon is active on any of the panels in the group, it will be active on the drawn panel
			if (other is Gizmo_QuadButtonPanel q) {
				if (groupedHandlers == null) groupedHandlers = new List<IDefensivePositionGizmoHandler>();
				groupedHandlers.Add(q.interactionHandler);
				activeIconMask |= q.activeIconMask;
			}
		}

		private void DrawHotKeyLabel(Rect gizmoRect) {
			var labelRect = new Rect(gizmoRect.x + ContentPadding, gizmoRect.y + ContentPadding, gizmoRect.width - 10f, 18f);
			var keyCode = hotKey.MainKey;
			Widgets.Label(labelRect, keyCode.ToStringReadable());
			GizmoGridDrawer.drawnHotKeys.Add(keyCode);
		}

		private void DrawGizmoLabel(string labelText, Rect gizmoRect) {
			var labelHeight = Text.CalcHeight(labelText, gizmoRect.width);
			labelHeight -= 2f;
			var labelRect = new Rect(gizmoRect.x, gizmoRect.yMax - labelHeight + 12f, gizmoRect.width, labelHeight);
			GUI.DrawTexture(labelRect, TexUI.GrayTextBG);
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(labelRect, labelText);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}
	}
}