using System;

namespace Pug.HttpApiClient
{
	public interface IApiClientFactory<out TClient> where TClient : IHttpApiClient
	{
		Uri Url { get; }

		TClient GetClient();
	}
}