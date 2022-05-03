using System.Threading.Tasks;

namespace Pug.HttpApiClient
{
	public interface IHttpClientDecorator
	{
		void Decorate( DecorationContext context );
		
		Task DecorateAsync( DecorationContext context );
	}
}