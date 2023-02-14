namespace Pug.HttpApiClient.OAuth2
{
	public interface ITokenExchangeSubjectTokenSource
	{
		string GetSubjectToken();
	}
}