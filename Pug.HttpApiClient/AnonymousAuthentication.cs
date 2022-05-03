using System.Threading.Tasks;

namespace Pug.HttpApiClient
{
	public sealed class AnonymousAuthentication : IHttpRequestMessageDecorator
	{
		public void Decorate( MessageDecorationContext context )
		{
		}

		public async Task DecorateAsync( MessageDecorationContext context )
		{
		}
	}
}