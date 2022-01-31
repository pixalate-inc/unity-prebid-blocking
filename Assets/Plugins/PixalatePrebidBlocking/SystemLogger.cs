using System;

namespace Pixalate.Mobile {
	internal sealed class SystemLogger : Logger {
		public SystemLogger ( LogLevel logLevel ) : base( logLevel ) {}

		internal override void Log ( LogLevel level, object message ) {
			if( logLevel >= level ) {
				Console.WriteLine( "[PixalateBlocking][" + level + "] " + message );
			}
		}

		internal override void LogInfo ( object message ) {
			if( logLevel >= LogLevel.Info ) {
				Console.WriteLine( "[PixalateBlocking][Info] " + message );
			}
		}

		internal override void LogDebug ( object message ) {
			if( logLevel >= LogLevel.Debug ) {
				Console.WriteLine( "[PixalateBlocking][Debug] " + message );
			}
		}

		internal override void LogWarning ( object message ) {
			if( logLevel >= LogLevel.Warning ) {
				Console.WriteLine( "[PixalateBlocking][Warning] " + message );
			}
		}

		internal override void LogError ( object message ) {
			if( logLevel >= LogLevel.Error ) {
				Console.WriteLine( "[PixalateBlocking][Error] " + message );
			}
		}
	}
}
