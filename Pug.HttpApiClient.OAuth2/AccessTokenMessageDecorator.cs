using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2
{
	public class AccessTokenMessageDecorator : IHttpRequestMessageDecorator
	{
		private readonly IAccessTokenManager<AccessToken> _accessTokenManager;

		public AccessTokenMessageDecorator( IAccessTokenManager<AccessToken> accessTokenManager )
		{
			_accessTokenManager = accessTokenManager ?? throw new ArgumentNullException( nameof(accessTokenManager) );
		}

		public virtual void Decorate( MessageDecorationContext context )
		{
			AccessToken accessToken = _accessTokenManager.GetAccessToken();

			context.RequestHeaders.Authorization = new AuthenticationHeaderValue( accessToken.TokenType, accessToken.Token );
		}

		public virtual async Task DecorateAsync( MessageDecorationContext context )
		{
			AccessToken accessToken = await _accessTokenManager.GetAccessTokenAsync().ConfigureAwait( false );

			context.RequestHeaders.Authorization = new AuthenticationHeaderValue( accessToken.TokenType, accessToken.Token );
		}
	}
}