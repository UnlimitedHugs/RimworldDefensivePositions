using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DefensivePositions {
	public class Gizmo_QuadButtonPanel : Command {
		private const float ContentPadding = 5f;
		private const float GizmoSize = 75f;
		private const float IconSize = 32f;

		private static readonly Color iconBaseColor = new Color(.5f, .5f, .5f, 1f);
		private static readonly Color iconMouseOverAdd = new Color(.1f, .1f, .1f, 0f);

		public override float Width {
			get { return GizmoSize; }
		}

		public Texture2D[] iconTextures;
		public Action hotkeyAction;
		public Action<int> iconClickAction;

		public override GizmoResult GizmoOnGUI(Vector2 topLeft) {
			var gizmoRect = new Rect(topLeft.x, topLeft.y, GizmoSize, GizmoSize);
			var contentRect = gizmoRect.ContractedBy(ContentPadding);
			Widgets.DrawWindowBackground(gizmoRect);
			var interacted = false;
			
			if (iconTextures != null) {
				for (int i = 0; i < iconTextures.Length; i++) {
					var iconTex = iconTextures[i];
					var iconOffset = new Vector2();
					switch (i) {
						case 1:
							iconOffset = new Vector2(IconSize, 0);
							break;
						case 2:
							iconOffset = new Vector2(0, IconSize);
							break;
						case 3:
							iconOffset = new Vector2(IconSize, IconSize);
							break;
					}
					var iconRect = new Rect(contentRect.x + iconOffset.x, contentRect.y + iconOffset.y, contentRect.width/2f, contentRect.height/2f);
					var iconColor = iconBaseColor;

					TooltipHandler.TipRegion(iconRect, string.Format(defaultDesc, i + 1));
					MouseoverSounds.DoRegion(iconRect, SoundDefOf.MouseoverCommand);
					if (Mouse.IsOver(iconRect)) {
						iconColor += iconMouseOverAdd;
					}
					if (Widgets.ButtonInvisible(iconRect, true)) {
						Event.current.button = i;
						interacted = true;
					}

					Graphics.DrawTexture(iconRect, iconTex, new Rect(0, 0, 1f, 1f), 0, 0, 0, 0, iconColor);
				}
			}

			DrawHotKeyLabel(gizmoRect);
			if (hotKey!=null && hotKey.KeyDownEvent) {
				interacted = true;
				Event.current.button = -1;
				Event.current.Use();
			}
			DrawGizmoLabel(defaultLabel, gizmoRect);
			return interacted ? new GizmoResult(GizmoState.Interacted, Event.current) : new GizmoResult(GizmoState.Clear);
		}

		public override void ProcessInput(Event ev) {
			if (activateSound != null) {
				activateSound.PlayOneShotOnCamera();
			}
			if (ev.button < 0) {
				if (hotkeyAction != null) hotkeyAction();
			} else {
				if (iconClickAction != null) iconClickAction(ev.button);
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