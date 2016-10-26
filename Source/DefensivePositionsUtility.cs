using UnityEngine;

namespace DefensivePositions {
	public static class DefensivePositionsUtility {
		public static bool ShiftIsHeld {
			get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
		}

		public static bool AltIsHeld {
			get { return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt); }
		}

		public static bool ControlIsHeld {
			get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand); }
		} 
	}
}