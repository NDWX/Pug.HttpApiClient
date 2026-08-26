using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Pug.HttpApiClient.Json;
using Xunit;

namespace Pug.HttpApiClient.Tests.NetStandard
{
	/// <summary>
	/// Guards the premise of this whole project: that it really is bound to the netstandard2.0
	/// compilation. If the reference silently resolved to net10.0 instead, every "legacy" test here
	/// would be re-testing the modern branch and quietly passing for the wrong reason.
	/// </summary>
	[Trait( "Category", "NetStandard" )]
	public class TargetFrameworkProbe
	{
		[Fact]
		public void LibraryIsTheNetStandardBuild()
		{
			TargetFrameworkAttribute attribute = typeof(HttpApiClient)
												.Assembly
												.GetCustomAttribute<TargetFrameworkAttribute>();

			Assert.Equal( ".NETStandard,Version=v2.0", attribute.FrameworkName );
		}

		[Fact]
		public void JsonExtensionsExposeTheNewtonsoftSurface()
		{
			// SetJsonSerializerSettings exists only under NETCOREAPP2_1 || NETSTANDARD; the modern build
			// exposes SetJsonSerializerOptions instead. Its presence proves which branch compiled.
			MethodInfo[] methods = typeof(IHttpApiClientJsonExtensions).GetMethods( BindingFlags.Public | BindingFlags.Static );

			Assert.Contains( methods, x => x.Name == "SetJsonSerializerSettings" );
			Assert.DoesNotContain( methods, x => x.Name == "SetJsonSerializerOptions" );
		}
	}
}
