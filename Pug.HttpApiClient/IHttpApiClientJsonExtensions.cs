using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
#if NETCOREAPP2_1 || NETSTANDARD
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace Pug.HttpApiClient.Json
{
	public static class IHttpApiClientJsonExtensions
	{
		private const string MediaTypeName = "application/json";

		public static async Task<T> GetAsync<T>(this IHttpApiClient httpApiClient, string path, IDictionary<string, string> headers = null, IDictionary<string, string> queries = null )
		{
			HttpResponseMessage response = await httpApiClient.GetAsync(
													path, new MediaTypeWithQualityHeaderValue( MediaTypeName ),
													headers, queries
												);

			if( !response.IsSuccessStatusCode ) throw new HttpApiRequestException( response );
			
			string openIdConfigurationString = await response.Content.ReadAsStringAsync();
			
#if NETCOREAPP2_1 || NETSTANDARD
			return JsonConvert.DeserializeObject<T>( openIdConfigurationString );
#else
			return JsonSerializer.Deserialize<T>( openIdConfigurationString );
#endif

		}

		public static Task<HttpResponseMessage> PostAsync<TContent>( this IHttpApiClient httpApiClient, string path, TContent content, IDictionary<string, string> headers = null,
																	IDictionary<string, string> queries = null )
		{
			HttpContent httpContent;
			string contentJson;

#if NETCOREAPP2_1 || NETSTANDARD
			contentJson = JsonConvert.SerializeObject( content );
#else
			contentJson = JsonSerializer.Serialize( content );
#endif

			httpContent = new StringContent( contentJson );

			return httpApiClient.PostAsync( path, httpContent, new MediaTypeWithQualityHeaderValue( MediaTypeName ), headers, queries );
		}

		public static Task<HttpResponseMessage> PutAsync<TContent>( this IHttpApiClient httpApiClient, string path, TContent content, IDictionary<string, string> headers = null,
																	IDictionary<string, string> queries = null )
		{
			HttpContent httpContent;
			string contentJson;

#if NETCOREAPP2_1 || NETSTANDARD
			contentJson = JsonConvert.SerializeObject( content );
#else
			contentJson = JsonSerializer.Serialize( content );
#endif

			httpContent = new StringContent( contentJson );

			return httpApiClient.PutAsync( path, httpContent, new MediaTypeWithQualityHeaderValue( MediaTypeName ), headers, queries );
		}

#if !NETSTANDARD
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

			return httpApiClient.PatchAsync( path, httpContent, new MediaTypeWithQualityHeaderValue( MediaTypeName ), headers, queries );
		}
#endif
	}
}