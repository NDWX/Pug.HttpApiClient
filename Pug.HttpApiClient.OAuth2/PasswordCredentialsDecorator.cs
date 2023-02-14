using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2
{
	public class PasswordCredentialsDecorator : IHttpRequestMessageDecorator
	{
		public PasswordCredentialsDecorator( Uri url, Uri oAuth2ProviderUrl, string clientId, string clientSecret,
											string username, string password, string oAuth2Scopes,
											IHttpClientFactory httpClientFactory )
		{
			
		}
		
		public void Decorate( MessageDecorationContext context )
		{
			throw new NotImplementedException();
		}

		public async Task DecorateAsync( MessageDecorationContext context )
		{
			throw new NotImplementedException();
		}
	}
}