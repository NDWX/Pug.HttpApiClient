using System.Runtime.Serialization;
#if !NETCOREAPP2_1
using System.Text.Json.Serialization;
#endif

namespace Pug.HttpApiClient.OAuth2Decorators
{
	public record OpenIdConfiguration
	{
		[DataMember(Name = "jwks_uri")]
#if !NETCOREAPP2_1
		[JsonPropertyName( "jwks_uri")]
#endif
		public string JwksUri { get; set; }
		
		[DataMember(Name="authorization_endpoint")]
#if !NETCOREAPP2_1
		[JsonPropertyName("authorization_endpoint")]
#endif
		public string AuthorizationEndpoint { get; set; }
		
		[DataMember(Name = "token_endpoint")]
#if !NETCOREAPP2_1
		[JsonPropertyName( "token_endpoint")]
#endif
		public string TokenEnndpoint { get; set; }
		
		[DataMember(Name="userinfo_endpoint")]
#if !NETCOREAPP2_1
		[JsonPropertyName("userinfo_endpoint")]
#endif
		public string UserInfoEndpoint { get; set; }
		
		[DataMember(Name="end_session_endpoint")]
#if !NETCOREAPP2_1
		[JsonPropertyName("end_session_endpoint")]
#endif
		public string EndSessionEndpoint { get; set; }
		
		[DataMember(Name="check_session_iframe")]
#if !NETCOREAPP2_1
		[JsonPropertyName("check_session_iframe")]
#endif
		public string CheckSessionIFrameEndpoint { get; set; }
		
		[DataMember(Name = "revocation_endpoint")]
#if !NETCOREAPP2_1
		[JsonPropertyName("revocation_endpoint")]
#endif
		public string RevocationEndpoint { get; set; }
		
		[DataMember(Name="introspection_endpoint")]
#if !NETCOREAPP2_1
		[JsonPropertyName("introspection_endpoint")]
#endif
		public string IntrospectionEndpoint { get; set; }
		
		[DataMember(Name="device_authorization_endpoint")]
#if !NETCOREAPP2_1
		[JsonPropertyName("device_authorization_endpoint")]
#endif
		public string DeviceAuthorizationEndpoint { get; set; }
		
		[DataMember(Name="frontchannel_logout_supported")]
#if !NETCOREAPP2_1
		[JsonPropertyName("frontchannel_logout_supported")]
#endif
		public bool FrontChannelLogoutSupported { get; set; }
		
		[DataMember(Name="frontchannel_logout_session_supported")]
#if !NETCOREAPP2_1
		[JsonPropertyName("frontchannel_logout_session_supported")]
#endif
		public bool FrontChannelLogoutSessionSupported { get; set; }
		
		[DataMember(Name="backchannel_logout_supported")]
#if !NETCOREAPP2_1
		[JsonPropertyName("backchannel_logout_supported")]
#endif
		public bool BackChannelLogoutSupported { get; set; }
		
		[DataMember(Name="backchannel_logout_session_supported")]
#if !NETCOREAPP2_1
		[JsonPropertyName("backchannel_logout_session_supported")]
#endif
		public bool BackChannelLogoutSessionSupported { get; set; }
		
		[DataMember(Name="scopes_supported")]
#if !NETCOREAPP2_1
		[JsonPropertyName("scopes_supported")]
#endif
		public string[] SupportedScope { get; set; }
		
		[DataMember(Name="claims_supported")]
#if !NETCOREAPP2_1
		[JsonPropertyName("claims_supported")]
#endif
		public string[] SupportedClaims { get; set; }
	}
}