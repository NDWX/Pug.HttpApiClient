using System;

namespace Pug.HttpApiClient.OAuth2
{
	public class StaticTokenExchangeSubjectTokenSource : ITokenExchangeSubjectTokenSource
	{
		private readonly string _subjectToken;

		public StaticTokenExchangeSubjectTokenSource( string subjectToken )
		{
			if( string.IsNullOrWhiteSpace( subjectToken ) )
				throw new ArgumentException( "Value cannot be null or whitespace.", nameof(subjectToken) );
			_subjectToken = subjectToken;
		}

		public string GetSubjectToken() => _subjectToken;
	}
}