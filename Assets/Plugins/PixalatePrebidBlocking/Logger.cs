namespace Pixalate.Mobile {
	internal abstract class Logger {
		public LogLevel logLevel;

		public Logger ( LogLevel logLevel ) {
			this.logLevel = logLevel;
		}

		internal abstract void Log ( LogLevel level, object message );
		internal abstract void LogInfo ( object message );
		internal abstract void LogDebug ( object message );
		internal abstract void LogWarning ( object message );
		internal abstract void LogError ( object message );
	}
}