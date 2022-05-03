using Pug.HttpApiClient;

namespace Pug.HttpApiClient
{
	public abstract class ApiClient
	{
		protected IHttpApiClient HttpClient { get; }

		protected ApiClient( IHttpApiClient httpClient )
		{
			HttpClient = httpClient;
		}
	}
}