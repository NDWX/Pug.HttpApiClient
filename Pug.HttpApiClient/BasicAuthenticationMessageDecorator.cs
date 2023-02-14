namespace Pug.HttpApiClient
{
	public sealed class BasicAuthenticationMessageDecorator : AuthorizationHeaderDecorator
	{
		public BasicAuthenticationMessageDecorator( string username, string password )
			: base( "Basic", Helpers.HttpBase64Encode( $"{username}:{password}" ) )
		{
		}
	}
}