#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace Pixalate.Mobile {
	internal sealed class UnityLogger : Logger {
		public UnityLogger ( LogLevel logLevel ) : base( logLevel ) {}
	
		internal override void Log ( LogLevel level, object message ) {
			if( logLevel >= level ) {
				Debug.Log( "[PixalateBlocking] " + message );
			}
		}

		internal override void LogInfo ( object message ) {
			if( logLevel >= LogLevel.Info ) {
				Debug.Log( "[PixalateBlocking] " + message );
			}
		}

		internal override void LogDebug ( object message ) {
			if( logLevel >= LogLevel.Debug ) {
				Debug.Log( "[PixalateBlocking] " + message );
			}
		}

		internal override void LogWarning ( object message ) {
			if( logLevel >= LogLevel.Warning ) {
				Debug.LogWarning( "[PixalateBlocking] " + message );
			}
		}

		internal override void LogError ( object message ) {
			if( logLevel >= LogLevel.Error ) {
				Debug.LogError( "[PixalateBlocking] " + message );
			}
		}
	}
}
#endif