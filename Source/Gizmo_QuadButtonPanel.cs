using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DefensivePositions {
	/**
	 * A gizmo with 4 separate interactable button areas.
	 */
	public class Gizmo_QuadButtonPanel : Command {
		private const float ContentPadding = 5f;
		private const float GizmoSize = 75f;
		private const float IconSize = 32f;

		private static readonly Color iconBaseColor = new Color(.5f, .5f, .5f, 1f);
		private static readonly Color iconMouseOverAdd = new Color(.1f, .1f, .1f, 0f);
		
		// must be static because GizmoOnGUI is called only for the first gizmo, but ProcessInput is called for all grouped gizmos
		private static int hoveredControlIndex;

		public Texture2D[] iconTextures;
		public Action hotkeyAction;
		public Action<int> iconClickAction;
		public ISlotAwareContextMenuProvider contextMenuProvider;

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions {
			get { return hoveredControlIndex >= 0 ? contextMenuProvider.AtSlot(hoveredControlIndex) : Enumerable.Empty<FloatMenuOption>(); }
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth) {
			var gizmoRect = new Rect(topLeft.x, topLeft.y, GizmoSize, GizmoSize);
			var contentRect = gizmoRect.ContractedBy(ContentPadding);
			Widgets.DrawWindowBackground(gizmoRect);
			var state = GizmoState.Clear;
			hoveredControlIndex = -1;

			if (iconTextures != null) {
				for (int i = 0; i < iconTextures.Length; i++) {
					var iconTex = iconTextures[i];
					var offset = new Vector2();
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
					}
					var iconRect = new Rect(contentRect.x + offset.x, contentRect.y + offset.y, contentRect.width / 2f, contentRect.height / 2f);
					var iconColor = iconBaseColor;

					TooltipHandler.TipRegion(iconRect, string.Format(defaultDesc, i + 1));
					MouseoverSounds.DoRegion(iconRect, SoundDefOf.Mouseover_Command);
					if (Mouse.IsOver(iconRect)) {
						state = GizmoState.Mouseover;
						iconColor += iconMouseOverAdd;
						hoveredControlIndex = i;
					}
					if (Widgets.ButtonInvisible(iconRect, true)) {
						hoveredControlIndex = i;
						state = Event.current.button == 0 ? GizmoState.Interacted : GizmoState.OpenedFloatMenu;
					}

					Graphics.DrawTexture(iconRect, iconTex, new Rect(0, 0, 1f, 1f), 0, 0, 0, 0, iconColor);
				}
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
				iconClickAction?.Invoke(hoveredControlIndex);
			} else {
				hotkeyAction?.Invoke();
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