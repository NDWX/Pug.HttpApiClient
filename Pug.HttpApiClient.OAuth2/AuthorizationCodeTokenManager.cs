using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Pug.HttpApiClient.OAuth2
{
	public sealed class AuthorizationCodeTokenManager : RefreshTokenManager
	{
		public AuthorizationCodeTokenManager( Uri oAuth2Endpoint, string clientId, string clientSecret, 
											string authorizationCode, string codeVerifier, string redirectUri,
											string oAuth2Scopes, IHttpClientFactory httpClientFactory) 
			: base( oAuth2Endpoint, clientId, clientSecret, oAuth2Scopes, httpClientFactory, 
					manager => GetAccessToken(manager, clientId, clientSecret, authorizationCode, codeVerifier, redirectUri) )
		{
		}

		private static RefreshableAccessToken GetAccessToken( RefreshTokenManager refreshTokenManager, string clientId, string clientSecret,
															string authorizationCode, string codeVerifier, string redirectUri )
		{
			FormUrlEncodedContent formUrlEncodedContent = new (
					new Dictionary<string, string>
					{
						["grant_type"] = "authorization_code",
						["code"] = authorizationCode,
						["client_id"] = clientId,
						["client_secret"] = clientSecret,
						["code_verifier"] = codeVerifier,
						["redirect_uri"] = redirectUri
					}
				);

			return refreshTokenManager.GetAccessToken( formUrlEncodedContent );
		}
	}
}