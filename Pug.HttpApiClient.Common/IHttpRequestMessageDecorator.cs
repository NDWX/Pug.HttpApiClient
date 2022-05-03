using System.Threading.Tasks;

namespace  Pug.HttpApiClient
{
	public interface IHttpRequestMessageDecorator
	{
		void Decorate(MessageDecorationContext context);
		
		Task DecorateAsync(MessageDecorationContext context);
	}
}
