using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;

namespace Pixalate.Mobile {
	public class HttpException : Exception {
		private HttpStatusCode statusCode;

		public HttpException ( HttpStatusCode statusCode, string description ) : base( description ) {
			this.statusCode = statusCode;
		}

		public override string ToString () {
			return statusCode + ": " + base.ToString();
		}
	}

	public class ApiResponseException : Exception {
		private int errorCode;

		public ApiResponseException ( int errorCode, string description ) : base( description ) {
			this.errorCode = errorCode;
		}

		public override string ToString () {
			return errorCode + ": " + base.ToString();
		}
	}

	public static class PixalateBlocking {
		private struct BlockingParameters {
			public string deviceId;
			public string ipv4;
			public string userAgent;
		}

		private struct BlockingApiResponse {
			public long time;

			public int errorCode;
			public string message;
			public float probability;
		}

		public delegate void BlockingResultHandler ( bool block, Exception error );

		static bool _initialized = false;

		static readonly Regex probabilityRegex = new Regex( "\"probability\"\\s*:\\s*(\\d+?)\\s*[,}]" );
		static readonly Regex messageRegex = new Regex( "\"message\"\\s*:\\s*\"(.*?)\"\\s*[,}]" );
		static readonly Regex statusRegex = new Regex( "\"status\"\\s*:\\s*(\\d+?)\\s*[,}]" );
		const string baseFraudURL = "https://api.pixalate.com/api/v2/fraud?";

		internal static HttpClient client;
		static Dictionary<BlockingParameters,BlockingApiResponse> cachedResults;

		private static Logger logger;

		public static LogLevel logLevel {
			get {
				return logger.logLevel;
			}
			set {
				if( !_initialized ) throw new InvalidOperationException( "You must call Initialize before setting the log level." );
				logger.logLevel = value;
			}
		}

		public static BlockingConfig options { get; private set; }

		public static bool initialized {
			get {
				return _initialized;
			}
		}

		public static void Initialize ( BlockingConfig options ) {
			PixalateBlocking.options = options;
#if UNITY_5_3_OR_NEWER || UNITY_ENGINE
			logger = new UnityLogger( LogLevel.Error );
#else
			logger = new SystemLogger( LogLevel.Error );
#endif
			_initialized = true;
			cachedResults = new Dictionary<BlockingParameters, BlockingApiResponse>();
			client = new HttpClient();
		}

		internal static void Log ( LogLevel level, object message ) {
			logger.Log( level, message );
		}

		internal static void LogInfo ( object message ) {
			logger.LogInfo( message );
		}

		internal static void LogDebug ( object message ) {
			logger.LogDebug( message );
		}

		internal static void LogWarning ( object message ) {
			logger.LogWarning( message );
		}

		internal static void LogError ( object message ) {
			logger.LogError( message );
		}

		public static void PerformBlockingRequest ( BlockingResultHandler handler ) {
			PerformBlockingRequest( BlockingMode.Default, handler );
		}

		public async static void PerformBlockingRequest ( BlockingMode mode, BlockingResultHandler handler ) {
			if( !_initialized ) throw new InvalidOperationException( "You must call Initialize before performing any blocking requests." );

			var timeout = options.requestTimeout;

			var tokenSource = new CancellationTokenSource();

			try {
				LogDebug( "Timeout: " + timeout );
				tokenSource.CancelAfter( timeout );

				LogDebug( "Starting tasks..." );

				var deviceIdSource = new TaskCompletionSource<string>();

				options.blockingStrategy.GetDeviceID( tokenSource.Token, value => {
					LogDebug( "Got device ID result: " + value );
					deviceIdSource.TrySetResult( value );
				});

				var ipv4Source = new TaskCompletionSource<string>();
				options.blockingStrategy.GetIPv4( tokenSource.Token, value => {
					LogDebug( "Got IPv4 result: " + value );
					ipv4Source.TrySetResult( value );
				});

				var userAgentSource = new TaskCompletionSource<string>();
				options.blockingStrategy.GetUserAgent( tokenSource.Token, value => {
					LogDebug( "Got user agent result: " + value );
					userAgentSource.TrySetResult( value );
				});

				tokenSource.Token.Register( () => {
					LogDebug( "Tasks have timed out." );
					deviceIdSource.SetCanceled();
					ipv4Source.SetCanceled();
					userAgentSource.SetCanceled();
				});

				LogDebug( "Started tasks, waiting for completion." );

				var results = await Task.WhenAll( new[] { deviceIdSource.Task, ipv4Source.Task, userAgentSource.Task } );

				LogDebug( "Completed all tasks." );

				var parameters = new BlockingParameters();

				parameters.deviceId = results[ 0 ];
				parameters.ipv4 = results[ 1 ];
				parameters.userAgent = results[ 2 ];

				BlockingApiResponse cachedResult;
				if( options.ttl > 0 && cachedResults.TryGetValue( parameters, out cachedResult ) ) {
					var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
					if( cachedResult.time > now ) {
						LogDebug( "Using cached results." );

						handler( cachedResult.probability < options.blockingThreshold, null );
						return;
					} else {
						cachedResults.Remove( parameters );
					}
				}

				if( mode != BlockingMode.Default ) {
					LogDebug( "Using debug mode, returning canned response: " + mode );
					handler( mode == BlockingMode.AlwaysBlock, null );
					return;
				}

				string requestUrl = buildUrl( parameters );

				LogDebug( requestUrl );

				var request = new HttpRequestMessage( HttpMethod.Get, requestUrl );

				request.Headers.Add( "X-Api-Key", options.apiKey );

				LogDebug( request.ToString() );

				using( HttpResponseMessage response = await client.SendAsync( request, tokenSource.Token ) ) {
					LogDebug( "Completed request: " + response.StatusCode + " " + response.ReasonPhrase );

					if( response.StatusCode != HttpStatusCode.OK ) {
						handler( false, new HttpException( response.StatusCode, response.ReasonPhrase ) );
						return;
					}

					using( var stream = await response.Content.ReadAsStreamAsync() )
					using( var reader = new StreamReader( stream ) ) {
						var str = await reader.ReadToEndAsync();

						LogDebug( str );

						var probabilityMatch = probabilityRegex.Match( str );

						float probability = -1;
						if( probabilityMatch.Length > 1 ) {
							if( !float.TryParse( probabilityMatch.Groups[ 1 ].Value, out probability ) ) probability = -1;
						}

						var statusMatch = statusRegex.Match( str );
						int errorCode = -1;
						if( statusMatch.Length > 1 ) {
							if( !int.TryParse( probabilityMatch.Groups[ 1 ].Value, out errorCode ) ) errorCode = -1;
						}

						var messageMatch = messageRegex.Match( str );
						string message = null;
						if( messageMatch.Length > 1 ) {
							message = probabilityMatch.Groups[ 1 ].Value;
						}

						if( errorCode > -1 ) {
							handler( false, new ApiResponseException( errorCode, message ) );
						} else {
							if( options.ttl > 0 ) {
								var result = new BlockingApiResponse();

								result.probability = probability;
								result.message = message;
								result.errorCode = errorCode;

								result.time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + options.ttl;

								cachedResults.Add( parameters, result );
							}

							handler( probability < options.blockingThreshold, null );
						}
					}
				}
			} catch ( OperationCanceledException ) {
				LogWarning( "Ran out of time to complete all strategies, defaulting to a non-blocking result." );
				handler( false, new OperationCanceledException( "Request timed out and was aborted." ) );
			} catch ( Exception exc ) {
				LogError( "An unknown error occurred, defaulting to a non-blocking result." );
				LogError( exc );
				handler( false, exc );
			} finally {
				tokenSource.Dispose();
			}
		}

		private static string buildUrl ( BlockingParameters parameters ) {
			var query = HttpUtility.ParseQueryString( string.Empty );

			if( parameters.ipv4 != null ) {
				query.Add( "ip", parameters.ipv4 );
			}

			if( parameters.userAgent != null ) {
				query.Add( "userAgent", parameters.userAgent );
			}

			if( parameters.deviceId != null ) {
				query.Add( "deviceId", parameters.deviceId );
			}

			return baseFraudURL + query.ToString();
		}
	}
}