using System;

namespace Pug.HttpApiClient
{
	public interface IApiClientFactory<out TClient>
	{
		Uri Url { get; }

		TClient GetClient();
	}
}