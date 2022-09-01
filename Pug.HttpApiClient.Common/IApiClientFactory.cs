using System;

namespace Pug.HttpApiClient
{
	public interface IApiClientFactory<TClient>
	{
		Uri Url { get; }

		TClient GetClient();
	}
}