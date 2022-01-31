namespace Pixalate.Mobile {
	public class BlockingConfig {
		private string _apiKey;
		private int _ttl;
		private float _blockingThreshold;
		private int _requestTimeout;
		private BlockingStrategy _blockingStrategy;

		public string apiKey {
			get {
				return _apiKey;
			}
		}

		public BlockingStrategy blockingStrategy {
			get {
				return _blockingStrategy;
			}
		}

		public int ttl {
			get {
				return _ttl;
			}
		}

		public float blockingThreshold {
			get {
				return _blockingThreshold;
			}
		}

		public int requestTimeout {
			get {
				return _requestTimeout;
			}

			set {
				_requestTimeout =  value ;
			}
		}

		private BlockingConfig () {

		}

		public class Builder {
			public readonly string apiKey;
			public BlockingStrategy blockingStrategy;
			public int ttl;
			public float blockingThreshold;
			public int requestTimeout;

			public Builder ( string apiKey ) {
				this.apiKey = apiKey;
				blockingThreshold = 0.75f;
				requestTimeout = 2000;
				ttl = 1000 * 60 * 60 * 8;
			}

			public Builder SetBlockingStrategy ( BlockingStrategy strategy ) {
				blockingStrategy = strategy;
				return this;
			}

			public Builder SetTTL ( int ttl ) {
				this.ttl = ttl;
				return this;
			}

			public Builder SetBlockingThreshold ( float blockingThreshold ) {
				this.blockingThreshold = blockingThreshold;
				return this;
			}

			public Builder SetRequestTimeout ( int requestTimeout ) {
				this.requestTimeout = requestTimeout;
				return this;
			}

			public BlockingConfig Build () {
				var config = new BlockingConfig();

				config._apiKey = apiKey;
				config._ttl = ttl;
				config._blockingThreshold = blockingThreshold;
				config.requestTimeout = requestTimeout;

				if( blockingStrategy != null ) {
					config._blockingStrategy = blockingStrategy;

					if( blockingStrategy is DefaultBlockingStrategy ) {
						DefaultBlockingStrategy defaultStrat = (DefaultBlockingStrategy)blockingStrategy;
						if( defaultStrat.requestTimeout <= 0 ) {
							defaultStrat.requestTimeout = requestTimeout;
						}
					}
				} else {
					config._blockingStrategy = new DefaultBlockingStrategy( ttl );
				}

				return config;
			}
		}
	}
}