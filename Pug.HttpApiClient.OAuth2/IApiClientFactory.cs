using System;

namespace Pug.HttpApiClient.OAuth2
{
	public interface IApiClientFactory<out TClient> 
		: Pug.HttpApiClient.IApiClientFactory<TClient>
		where TClient : ApiClient
	{
		Uri OAuth2ProviderUrl { get; }
		
		TClient GetClient( ITokenExchangeSubjectTokenSource tokenExchangeSubjectTokenSource );
		
		TClient GetClient( RefreshableAccessToken refreshableAccessToken );
		
		[Obsolete("Use authorization code flow instead where possible")]
		TClient GetClient( string username, string password );
	}
}