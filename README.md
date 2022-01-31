Pixalate Pre-Bid Fraud Blocking SDK for Unity
===

- [Pixalate Pre-Bid Fraud Blocking SDK for Unity](#pixalate-pre-bid-fraud-blocking-sdk-for-unity)
  - [Installation & Integration](#installation--integration)
    - [.unitypackage](#unitypackage)
  - [Authentication & Basic Configuration](#authentication--basic-configuration)
  - [Blocking Ads](#blocking-ads)
    - [Testing Responses](#testing-responses)
  - [Logging](#logging)
  - [Advanced Configuration](#advanced-configuration)
    - [Blocking Strategies](#blocking-strategies)
      - [Device ID](#device-id)
      - [IPv4 Address](#ipv4-address)
      - [User Agent](#user-agent)
      - [Parameter Caching](#parameter-caching)
    - [Custom Blocking Strategies](#custom-blocking-strategies)
      - [Overriding DefaultBlockingStategy](#overriding-defaultblockingstategy)
      - [Creating a Strategy From Scratch](#creating-a-strategy-from-scratch)

The Pixalate Pre-Bid Blocking SDK provides easy an easy implementation of Pixalate's Pre-Bid Fraud Blocking API in your Unity application.

## Installation & Integration

### .unitypackage

Download the latest `.unitypackage` file from the releases section of the repository. Double-click it while your project is open to import the files into your project. They will automatically be extracted into your project.

## Authentication & Basic Configuration

To use the Pixalate Blocking SDK, you must first initialize it. You can do this by calling `PixalateBlocking.Initialize()` and passing in a configuration object with your API key.

```cs
// use the namespace at the top of your file
using Pixalate.Mobile;

// ...

// in your app initialization code, such as MainActivity.java
// A sample configuration & initialization -- the values chosen for this example
// are not meaningful.
BlockingConfig config = new BlockingConfig.Builder("your-api-key-goes-here")
    .SetBlockingThreshold(0.8)
    .SetTTL(1000 * 60 * 60 * 7)
    .SetRequestTimeout(3000)
    .Build();

PixalateBlocking.Initialize(config);
```

The configuration builder allows you to override the default configuration values:

Parameter Name    | Description | Default Value 
------------------|-------------|---------------:
blockingThreshold | The probability threshold at which blocking should occur.<br/>Normal range is anywhere from 0.75-0.9. | 0.75
ttl               | How long results should be cached before making another request. | 8 hours
requestTimeout    | How long requests are allowed to run before aborting. In the rare case of a network issue, this will help ensure the Pixalate SDK is not a bottleneck to running your ads. | 2 seconds
blockingStrategy  | The blocking strategy used to retrieve device parameters such as device id and IP address | `DefaultBlockingStrategy`


## Blocking Ads

Once the SDK is set up, you can implement it into your ad loading logic. The SDK is framework and approach-agnostic.

The basic pattern for performing a block request is to call RequestBlockStatus while providing a callback. Inside the callback, you can check whether the request was blocked or not, and act accordingly.

```cs
// The most basic blocking request, displaying 
// all possible interface implementations.
// You only need to implement the methods you need for your use case.
PixalateBlocking.RequestBlockStatus(( block, error ) => {
  if( error != null ) {
    // there was some kind of error. `block` will always be false if there is an error.
  }
  
  if( block ) {
    // the ad load should be blocked, don't load the ad.
  } else {
    // the ad load was not blocked. Load the ad here!
  }
});
```

### Testing Responses

During development, it may be helpful to test both blocked and unblocked behavior. You can accomplish this using the alternate overload for `Pixalate.RequestBlockStatus` that includes a `BlockingMode` parameter. You can pass `BlockingMode.Default` to use normal behavior, `BlockingMode.AlwaysBlock` to simulate a blocked response, or `BlockingMode.NeverBlock` to simulate a non-blocked response:


```cs
// Pass the blocking mode as the first parameter to simulate different blocking conditions.
PixalateBlocking.RequestBlockStatus(
  BlockingMode.AlwaysBlock, 
  new BlockingStatusListener () { /* ... */ }
);
```

Debug mode requests execute normally except that they do not perform a real API call, and so can be used to test custom blocking strategies as well.

## Logging

The SDK supports multiple logging levels which can provide additional context when debugging. The current level can be set through `Pixalate.setLogLevel`, and defaults to `INFO`. Logging can be disabled entirely by setting the level to `NONE`.

```cs
PixalateBlocking.logLevel = LogLevel.Debug;
```

## Advanced Configuration

### Blocking Strategies

Pixalate provides default strategies for both the device ID and IPv4 address parameters. These values should cover most common use cases. 

If for any reason you wish to add, remove, or modify the blocking strategies used by the library, you can create a custom strategy. This is explained in more detail below.

#### Device ID

This will call [Application.RequestAdvertisingIdentifierAsync](https://docs.unity3d.com/ScriptReference/Application.RequestAdvertisingIdentifierAsync.html) and return the result. If this method is not returning the values you need, it is recommended to override the default behavior and connect it to your own Android or iOS code.

#### IPv4 Address

The SDK will retrieve the external IPv4 address of the device by utilizing a Pixalate endpoint.  

#### User Agent

Although the pre-bid fraud API supports passing browser user agents, the concept of a user agent is nebulous when in an app context. For this reason, the default blocking strategy does not utilize user agents.

#### Parameter Caching

The default blocking strategy has utilities for caching the parameters it retrieves. By default, it will mirror the TTL of the global configuration. This value can be overridden by passing a new DefaultBlockingStrategy object to the BlockingConfig.Builder, as shown in the snippet.

```java
// Override the TTL of the default blocking strategy when constructing 
// the blocking config object if you want to set it to something 
// different than the global configuration's TTL.
BlockingConfig config = new BlockingConfig.Builder("my-api-key")
    .SetBlockingStrategy(new DefaultBlockingStrategy(1000 * 60 * 5))
    .Build();
``` 

### Custom Blocking Strategies

If you have an alternate use case that the default strategies are not providing, you would like more control over how you retrieve the blocking parameters, or if you want to add or remove included parameters, you can create your own blocking strategy.

#### Overriding DefaultBlockingStategy

The simplest method is to extend `DefaultBlockingStrategy`, which carries over all default behavior, including caching.

When extending the DefaultBlockingStrategy, make sure to override the `-Impl` variety methods rather than the interface methods so as to preserve caching behavior.

```cs
// TestBlockingStrategy.cs
static class TestBlockingStrategy : DefaultBlockingStrategy {
    public override void GetIPv4Impl (CancellationToken token, Action<string> callback) {
        callback.done( null );
    }
}

// Then, in your initialization code, pass your modified strategy
// into the SetBlockingStrategy builder method.
BlockingConfig config = new BlockingConfig.Builder("my-api-key")
    .SetBlockingStrategy(new TestBlockingStrategy())
    .Build();
```

#### Creating a Strategy From Scratch

To create a custom strategy from scratch, you must extend the `BlockingStrategy` interface. All of the methods have default implementations returning null, meaning you only need to override the strategies you want to provide values for.

```cs
// CustomBlockingStrategy.cs

// A contrived custom strategy only implementing the IPv4 parameter.
static class CustomBlockingStrategy : BlockingStrategy {
    public async void GetIPv4 (CancellationToken token, Action<string> callback) {
        // The strategy implementations are executed in a background thread, so it is OK 
        // to use blocking operations such as HttpsURLConnection.

			var request = new HttpRequestMessage(HttpMethod.Get, "some-ipv4-endpoint");

			using(HttpResponseMessage response = await new HttpClient().SendAsync( request, token )) {
				string ipv4 = /* get ipv4 from response */;
        callback( ipv4 );
      }
    }

    public void GetDeviceID (CancellationToken token, Action<string> callback) {
      callback( null );
    }

    public void GetUserAgent (CancellationToken token, Action<string> callback) {
      callback( null );
    }
}
```

```cs
// Then, in your initialization code, pass your modified strategy
// into the setBlockingStrategy builder method.
BlockingConfig config = new BlockingConfig.Builder("my-api-key")
    .SetBlockingStrategy(new CustomBlockingStrategy())
    .Build();
```

**Important note:** To keep the core functionality as implementation agnostic as possible, default strategy caching behavior is self-contained within the `DefaultBlockingStrategy` class. If you implement your own blocking strategy from scratch using the `BlockingStrategy` interface, you will need to manage your own caching of parameters. The caching of API responses is always managed by the SDK, and is unaffected by the blocking strategy.
