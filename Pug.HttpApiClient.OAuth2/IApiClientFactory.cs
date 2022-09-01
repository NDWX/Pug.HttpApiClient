using System;
using Pug.HttpApiClient.OAuth2Decorators;

namespace Pug.HttpApiClient.OAuth2
{
	public interface IApiClientFactory<TClient> : Pug.HttpApiClient.IApiClientFactory<TClient>
	{
		Uri OAuth2ProviderUrl { get; }
		
		string ClientId { get; }

		TClient GetClient( ITokenExchangeSubjectTokenSource tokenExchangeSubjectTokenSource );
	}
}