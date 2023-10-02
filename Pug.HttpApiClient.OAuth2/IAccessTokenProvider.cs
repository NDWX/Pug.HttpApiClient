using System.Threading.Tasks;

namespace Pug.HttpApiClient.OAuth2
{
	public interface IAccessTokenProvider<TAccessToken>
	{
		TAccessToken GetAccessToken();
		
		Task<TAccessToken> GetAccessTokenAsync();
		
	}
}