using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Pug.HttpApiClient.OAuth2
{
	public sealed class PasswordAccessTokenManager : RefreshTokenManager
	{
		public new const string GrantType = "password";
		
		public PasswordAccessTokenManager(  Uri oAuth2ProviderUrl, string clientId, string clientSecret,
											string username, string password, string oAuth2Scopes,
											IHttpClientFactory httpClientFactory ) 
			: base( oAuth2ProviderUrl, clientId, clientSecret, oAuth2Scopes, httpClientFactory, 
					m => GetNewAccessToken(m, username, password, oAuth2Scopes) )
		{
		}

		private static RefreshableAccessToken GetNewAccessToken(RefreshTokenManager refreshTokenManager, string username, string password, string scopes)
		{
			FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(
					new Dictionary<string, string>
					{
						["grant_type"] = GrantType,
						["scope"] = scopes,
						["username"] = username,
						["password"] = password
					}
				);

			return refreshTokenManager.GetAccessToken( formUrlEncodedContent );
		}
	}
}