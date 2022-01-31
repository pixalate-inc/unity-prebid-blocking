using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace Pixalate.Mobile {
	public class DefaultBlockingStrategy : BlockingStrategy {
		static readonly Regex ipRegex = new Regex( "\"ip\"\\s*:\\s*\"(.*?)\"\\s*[,}]" );

		private string _cachedDeviceId;
		private long _nextDeviceIdFetchTime;

		private string _cachedIpv4Address;
		private long _nextIpv4AddressFetchTime;

		private string _cachedUserAgent;
		private long _nextUserAgentFetchTime;

		private int _requestTimeout = -1;
		private int _cacheTTL;

		public int requestTimeout {
			get {
				return _requestTimeout;
			}
			set {
				if( value < 0 ) throw new ArgumentOutOfRangeException( "requestTimeout", requestTimeout, "must be greater than 0" );
				_requestTimeout = value;
			}
		}

		public int cacheTTL {
			get {
				return _cacheTTL;
			}
			set {
				_cacheTTL = value;
			}
		}

		public DefaultBlockingStrategy ( int cacheTTL ) {
			this.cacheTTL = cacheTTL;
		}

		public DefaultBlockingStrategy ( int cacheTTL, int requestTimeout ) {
			this.cacheTTL = cacheTTL;
			this.requestTimeout = requestTimeout;
		}

		public override void GetDeviceID ( CancellationToken token, Action<string> callback ) {
			if( cacheTTL > 0 ) {
				PixalateBlocking.LogDebug( "Checking device ID cache..." );
				long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				if( _nextDeviceIdFetchTime > now && _cachedDeviceId != null ) {
					PixalateBlocking.LogDebug( "Using cached deviceID: " + _cachedDeviceId );
					callback( _cachedDeviceId );
				} else {
					PixalateBlocking.LogDebug( "Cache missed, fetching device ID..." );
					GetDeviceIDImpl( token, result => {
						PixalateBlocking.LogDebug( "Fetched deviceID: " + result );
						if( result != null ) {
							_cachedDeviceId = result;
							_nextDeviceIdFetchTime = now + cacheTTL;
						}
						callback( result );
					});
				}
			} else {
				PixalateBlocking.LogDebug( "Cache is disabled, fetching device ID..." );
				GetDeviceIDImpl( token, callback );
			}
		}

		public virtual void GetDeviceIDImpl ( CancellationToken token, Action<string> callback ) {
#if ( !UNITY_EDITOR && UNITY_5_3_OR_NEWER ) || UNITY_ENGINE
			PixalateBlocking.LogDebug( "FETCHING DEVICE ID" );
			try {
				var success = Application.RequestAdvertisingIdentifierAsync( ( advertisingId, trackingEnabled, error ) => {
					PixalateBlocking.LogDebug( "Advertising ID: " + advertisingId );
					if( advertisingId != null ) {
						callback( advertisingId );
					}
				});

				PixalateBlocking.LogDebug( "Device ID SUCCESS: " + success );

				if( !success ) callback( null );
			} catch ( Exception exc ) {
				PixalateBlocking.LogDebug( "ERR: " + exc );
			}
#elif UNITY_EDITOR
			PixalateBlocking.LogDebug( "Device ID not supported in the Editor." );
			callback( null );
#else
			PixalateBlocking.LogDebug( "Device ID fetching is not supported in the current environment." );
			callback( null );
#endif
		}

		public override void GetIPv4 ( CancellationToken token, Action<string> callback ) {
			if( cacheTTL > 0 ) {
				PixalateBlocking.LogDebug( "Checking IPv4 cache..." );
				long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				if( _nextIpv4AddressFetchTime > now && _cachedIpv4Address != null ) {
					PixalateBlocking.LogDebug( "Using cached IPv4: " + _cachedIpv4Address );
					callback( _cachedIpv4Address );
				} else {
					PixalateBlocking.LogDebug( "Cache missed, fetching IPv4..." );
					GetIPv4Impl( token, result => {
						PixalateBlocking.LogDebug( "Fetched IPv4: " + result );
						if( result != null ) {
							_cachedIpv4Address = result;
							_nextIpv4AddressFetchTime = now + cacheTTL;
						}
						callback( result );
					} );
				}
			} else {
				PixalateBlocking.LogDebug( "Cache is disabled, fetching IPv4..." );
				GetIPv4Impl( token, callback );
			}
		}

		public async virtual void GetIPv4Impl ( CancellationToken token, Action<string> callback ) {
			var ipEndpoint = "https://get-ipv4.adrta.com";
			var request = new HttpRequestMessage( HttpMethod.Get, ipEndpoint );

			using( HttpResponseMessage response = await PixalateBlocking.client.SendAsync( request, token ) ) {
				if( response.StatusCode != HttpStatusCode.OK ) {
					PixalateBlocking.LogError( "Failed to fetch IPv4: " + response.StatusCode + " " + response.ReasonPhrase );
					callback( null );
					return;
				}

				using( var stream = await response.Content.ReadAsStreamAsync() )
				using( var reader = new StreamReader( stream ) ) {
					var str = await reader.ReadToEndAsync();

					var ipMatch = ipRegex.Match( str );

					string ip = null;
					if( ipMatch.Length > 1 ) {
						ip = ipMatch.Groups[ 1 ].Value;
					}

					callback( ip );
				}
			}
		}

		public override void GetUserAgent ( CancellationToken token, Action<string> callback ) {
			if( cacheTTL > 0 ) {
				PixalateBlocking.LogDebug( "Checking user agent cache..." );
				long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				if( _nextUserAgentFetchTime > now && _cachedUserAgent != null ) {
					PixalateBlocking.LogDebug( "Using cached user agent: " + _cachedUserAgent );
					callback( _cachedUserAgent );
				} else {
					PixalateBlocking.LogDebug( "Cache missed, fetching user agent..." );
					GetUserAgentImpl( token, result => {
						PixalateBlocking.LogDebug( "Fetched user agent: " + result );
						if( result != null ) {
							_cachedUserAgent = result;
							_nextUserAgentFetchTime = now + cacheTTL;
						}
						callback( result );
					} );
				}
			} else {
				PixalateBlocking.LogDebug( "Cache is disabled, fetching user agent..." );
				GetUserAgentImpl( token, callback );
			}
		}

		public virtual void GetUserAgentImpl ( CancellationToken token, Action<string> callback ) {
			callback( null );
		}
	}
}