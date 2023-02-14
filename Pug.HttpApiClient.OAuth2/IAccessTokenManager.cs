using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2
{
	public interface IAccessTokenManager<TAccessToken>
	{
		TAccessToken GetAccessToken();
		Task<TAccessToken> GetAccessTokenAsync();
		
	}
}