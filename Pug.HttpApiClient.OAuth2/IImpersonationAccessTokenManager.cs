using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	public interface IImpersonationAccessTokenManager
	{
		AccessToken GetAccessToken();
		Task<AccessToken> GetAccessTokenAsync();
	}
}