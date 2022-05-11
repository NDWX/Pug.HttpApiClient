using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
#if NETCOREAPP2_1
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace Pug.HttpApiClient.Json
{
	public static class IHttpApiClientJsonExtensions
	{
		public static async Task<T> GetAsync<T>(this IHttpApiClient httpApiClient, string path, IDictionary<string, string> headers = null, IDictionary<string, string> queries = null )
		{
			HttpResponseMessage response = await httpApiClient.GetAsync(
													path, new MediaTypeWithQualityHeaderValue( MediaTypeNames.Application.Json ),
													queries, headers
												);

			if( response.IsSuccessStatusCode )
			{
				string openIdConfigurationString = await response.Content.ReadAsStringAsync();
#if NETCOREAPP2_1
				return JsonConvert.DeserializeObject<T>( openIdConfigurationString );
#else
				return JsonSerializer.Deserialize<T>( openIdConfigurationString );
#endif
			}

			throw new HttpApiRequestException( response );
		}

		public static Task<HttpResponseMessage> PostAsync<TContent>( this IHttpApiClient httpApiClient, string path, TContent content, IDictionary<string, string> headers = null,
																	IDictionary<string, string> queries = null )
		{
			HttpContent httpContent;
			string contentJson;

#if NETCOREAPP2_1
			contentJson = JsonConvert.SerializeObject( content );
#else
			contentJson = JsonSerializer.Serialize( content );
#endif

			httpContent = new StringContent( contentJson );

			return httpApiClient.PostAsync( path, httpContent, new MediaTypeWithQualityHeaderValue( MediaTypeNames.Application.Json ), headers, queries );
		}

		public static Task<HttpResponseMessage> PutAsync<TContent>( this IHttpApiClient httpApiClient, string path, TContent content, IDictionary<string, string> headers = null,
																	IDictionary<string, string> queries = null )
		{
			HttpContent httpContent;
			string contentJson;

#if NETCOREAPP2_1
			contentJson = JsonConvert.SerializeObject( content );
#else
			contentJson = JsonSerializer.Serialize( content );
#endif

			httpContent = new StringContent( contentJson );

			return httpApiClient.PutAsync( path, httpContent, new MediaTypeWithQualityHeaderValue( MediaTypeNames.Application.Json ), headers, queries );
		}

		public static Task<HttpResponseMessage> PatchAsync<TContent>( this IHttpApiClient httpApiClient, string path, TContent content, IDictionary<string, string> headers = null,
																	IDictionary<string, string> queries = null )
		{
			HttpContent httpContent;
			string contentJson;

#if NETCOREAPP2_1
			contentJson = JsonConvert.SerializeObject( content );
#else
			contentJson = JsonSerializer.Serialize( content );
#endif

			httpContent = new StringContent( contentJson );

			return httpApiClient.PatchAsync( path, httpContent, new MediaTypeWithQualityHeaderValue( MediaTypeNames.Application.Json ), headers, queries );
		}
	}
}