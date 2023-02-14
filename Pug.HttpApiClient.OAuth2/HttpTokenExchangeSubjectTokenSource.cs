using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Pug.HttpApiClient.OAuth2
{
	public class HttpTokenExchangeSubjectTokenSource : ITokenExchangeSubjectTokenSource
	{
		private readonly IHttpContextAccessor _httpContextAccessor;

		public HttpTokenExchangeSubjectTokenSource( IHttpContextAccessor httpContextAccessor)
		{
			_httpContextAccessor = httpContextAccessor;
		}
		
		public string GetSubjectToken()
		{
			if( !_httpContextAccessor.HttpContext.Request.Headers.TryGetValue( "Authorization", out StringValues values ) 
				|| values.Count < 1 || !values.First().StartsWith( "BEARER ", StringComparison.InvariantCultureIgnoreCase ))
				return null;

			string value = values.First();

			int separatorIndex = value.IndexOf( " ", StringComparison.InvariantCulture );
			
			return value.Substring( separatorIndex, value.Length - ( separatorIndex + 1 ) );
		}
	}
}