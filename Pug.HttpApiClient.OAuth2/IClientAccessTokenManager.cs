using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	public interface IClientAccessTokenManager
	{
		ClientAccessToken GetAccessToken();
		Task<ClientAccessToken> GetAccessTokenAsync();
	}
}