using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Pug.HttpApiClient.OAuth2;
using Xunit;

namespace Pug.HttpApiClient.Tests.OAuth2
{
	public class AccessTokenManagerTests
	{
		/// <summary>
		/// Counts fetches so tests can pin the base class's caching/serialization behaviour without
		/// any real network traffic - <see cref="AccessTokenManager{TToken}"/> never touches the
		/// discovery/token hops itself, only the concrete managers do.
		/// </summary>
		private sealed class FakeAccessTokenManager : AccessTokenManager<AccessToken>
		{
			private readonly Func<AccessToken> _fetch;
			private readonly Func<Task<AccessToken>> _fetchAsync;

			public int SyncFetchCount { get; private set; }
			public int AsyncFetchCount { get; private set; }

			public FakeAccessTokenManager( Func<AccessToken> fetch, Func<Task<AccessToken>> fetchAsync = null, AccessToken seed = null )
				: base( new Uri( "https://auth.example.com" ), Mock.Of<IHttpClientFactory>(), seed )
			{
				_fetch = fetch;
				_fetchAsync = fetchAsync ?? ( () => Task.FromResult( fetch() ) );
			}

			protected override AccessToken GetNewAccessToken()
			{
				SyncFetchCount++;

				return _fetch();
			}

			protected override async Task<AccessToken> GetNewAccessTokenAsync()
			{
				AsyncFetchCount++;

				return await _fetchAsync();
			}
		}

		private static AccessToken NewToken( int validityPeriod = 3600 ) =>
			new () { Token = "t", TokenType = "Bearer", ValidityPeriod = validityPeriod };

		[Fact]
		public void GetAccessToken_FirstCall_FetchesNewToken()
		{
			FakeAccessTokenManager manager = new ( () => NewToken() );

			AccessToken token = manager.GetAccessToken();

			Assert.Equal( "t", token.Token );
			Assert.Equal( 1, manager.SyncFetchCount );
		}

		[Fact]
		public void GetAccessToken_SecondCallWithinValidity_ServedFromCache()
		{
			FakeAccessTokenManager manager = new ( () => NewToken() );

			manager.GetAccessToken();
			manager.GetAccessToken();

			Assert.Equal( 1, manager.SyncFetchCount );
		}

		[Fact]
		public void GetAccessToken_AfterExpiry_RefetchesToken()
		{
			// A 1 second ValidityPeriod minus the 5s safety margin is already expired the instant it
			// is returned, so no sleep is needed to force a refetch on the very next call.
			FakeAccessTokenManager manager = new ( () => NewToken( 1 ) );

			manager.GetAccessToken();
			manager.GetAccessToken();

			Assert.Equal( 2, manager.SyncFetchCount );
		}

		[Fact]
		public async Task GetAccessTokenAsync_FirstCall_FetchesNewToken()
		{
			FakeAccessTokenManager manager = new ( () => NewToken() );

			AccessToken token = await manager.GetAccessTokenAsync();

			Assert.Equal( "t", token.Token );
			Assert.Equal( 1, manager.AsyncFetchCount );
		}

		[Fact]
		public async Task GetAccessTokenAsync_SecondCallWithinValidity_ServedFromCache()
		{
			FakeAccessTokenManager manager = new ( () => NewToken() );

			await manager.GetAccessTokenAsync();
			await manager.GetAccessTokenAsync();

			Assert.Equal( 1, manager.AsyncFetchCount );
		}

		[Fact]
		public async Task GetAccessTokenAsync_AfterExpiry_RefetchesToken()
		{
			FakeAccessTokenManager manager = new ( () => NewToken( 1 ) );

			await manager.GetAccessTokenAsync();
			await manager.GetAccessTokenAsync();

			Assert.Equal( 2, manager.AsyncFetchCount );
		}

		[Fact]
		public async Task GetAccessToken_ThenGetAccessTokenAsync_SharesCacheAcrossSyncAndAsync()
		{
			FakeAccessTokenManager manager = new ( () => NewToken() );

			manager.GetAccessToken();
			AccessToken second = await manager.GetAccessTokenAsync();

			Assert.Equal( "t", second.Token );
			Assert.Equal( 1, manager.SyncFetchCount );
			Assert.Equal( 0, manager.AsyncFetchCount );
		}

		[Fact]
		public async Task GetAccessTokenAsync_ThenGetAccessToken_SharesCacheAcrossSyncAndAsync()
		{
			FakeAccessTokenManager manager = new ( () => NewToken() );

			await manager.GetAccessTokenAsync();
			AccessToken second = manager.GetAccessToken();

			Assert.Equal( "t", second.Token );
			Assert.Equal( 1, manager.AsyncFetchCount );
			Assert.Equal( 0, manager.SyncFetchCount );
		}

		[Fact]
		public async Task GetAccessToken_ConcurrentCallers_FetchExactlyOnce()
		{
			// Sleeping inside the fetch keeps callers overlapping without any flaky timing
			// assertions: the SemaphoreSlim in the base class either serializes them to a single
			// fetch, or it doesn't - the fetch count settles the question deterministically.
			FakeAccessTokenManager manager = new ( () =>
				{
					Thread.Sleep( 100 );

					return NewToken();
				} );

			Task<AccessToken>[] callers = Enumerable.Range( 0, 8 )
														.Select( _ => Task.Run( () => manager.GetAccessToken() ) )
														.ToArray();

			AccessToken[] results = await Task.WhenAll( callers );

			Assert.Equal( 1, manager.SyncFetchCount );
			Assert.All( results, token => Assert.Equal( "t", token.Token ) );
		}

		[Fact]
		public async Task GetAccessTokenAsync_ConcurrentCallers_FetchExactlyOnce()
		{
			FakeAccessTokenManager manager = new (
					fetch: () => NewToken(),
					fetchAsync: async () =>
					{
						await Task.Delay( 100 );

						return NewToken();
					}
				);

			Task<AccessToken>[] callers = Enumerable.Range( 0, 8 )
														.Select( _ => manager.GetAccessTokenAsync() )
														.ToArray();

			AccessToken[] results = await Task.WhenAll( callers );

			Assert.Equal( 1, manager.AsyncFetchCount );
			Assert.All( results, token => Assert.Equal( "t", token.Token ) );
		}
		private static FakeAccessTokenManager ManagerSeededWith( AccessToken seed ) =>
			new ( () => new AccessToken { Token = "refetched", TokenType = "Bearer", ValidityPeriod = 3600 }, seed: seed );

		[Fact]
		public void SeededAccessToken_AlreadyExpired_IsReplacedOnFirstUse()
		{
			// Regression: the constructor initialised the expiry to DateTime.MaxValue, so a token supplied
			// to the constructor was cached forever however short its expires_in was. ValidityPeriod 1,
			// minus the 5s safety margin, is already in the past - the seed must be discarded at once.
			FakeAccessTokenManager manager =
				ManagerSeededWith( new AccessToken { Token = "seed", TokenType = "Bearer", ValidityPeriod = 1 } );

			AccessToken result = manager.GetAccessToken();

			Assert.Equal( "refetched", result.Token );
			Assert.Equal( 1, manager.SyncFetchCount );
		}

		[Fact]
		public async Task SeededAccessToken_AlreadyExpired_IsReplacedOnFirstAsyncUse()
		{
			FakeAccessTokenManager manager =
				ManagerSeededWith( new AccessToken { Token = "seed", TokenType = "Bearer", ValidityPeriod = 1 } );

			AccessToken result = await manager.GetAccessTokenAsync();

			Assert.Equal( "refetched", result.Token );
			Assert.Equal( 1, manager.AsyncFetchCount );
		}

		[Fact]
		public void SeededAccessToken_StillWithinValidityPeriod_IsServedFromCacheWithoutFetching()
		{
			// The complementary case, guarding against over-correcting the above: expiring seeded tokens
			// on their own ValidityPeriod must not make a seed with a long validity useless.
			FakeAccessTokenManager manager =
				ManagerSeededWith( new AccessToken { Token = "seed", TokenType = "Bearer", ValidityPeriod = 3600 } );

			AccessToken result = manager.GetAccessToken();

			Assert.Equal( "seed", result.Token );
			Assert.Equal( 0, manager.SyncFetchCount );
		}
	}
}