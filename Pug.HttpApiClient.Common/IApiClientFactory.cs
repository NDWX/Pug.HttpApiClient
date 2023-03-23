using System;

namespace Pug.HttpApiClient
{
	public interface IApiClientFactory<out TClient> where TClient : ApiClient
	{
		Uri Url { get; }

		TClient GetClient();
	}
}