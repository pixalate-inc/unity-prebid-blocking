using System;
using System.Threading;

namespace Pixalate.Mobile {
	public abstract class BlockingStrategy {
		public virtual void GetDeviceID ( CancellationToken token, Action<string> callback ) {
			callback( null );
		}

		public virtual void GetIPv4 ( CancellationToken token, Action<string> callback ) {
			callback( null );
		}

		public virtual void GetUserAgent ( CancellationToken token, Action<string> callback ) {
			callback( null );
		}
	}
}