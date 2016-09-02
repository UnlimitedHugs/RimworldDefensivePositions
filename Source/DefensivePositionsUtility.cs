using System;
using System.Reflection;

namespace DefensivePositions {
	public static class DefensivePositionsUtility {
		private const string logPrefix = "[DefensivePositions] ";

		public static void Log(object message) {
			Verse.Log.Message(logPrefix + message);
		}

		public static void Error(object message) {
			Verse.Log.Error(logPrefix + message);
		}
	}
}