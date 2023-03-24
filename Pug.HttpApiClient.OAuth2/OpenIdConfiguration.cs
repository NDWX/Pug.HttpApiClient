using System.Runtime.Serialization;
using Newtonsoft.Json;
#if !(NETCOREAPP2_1 || NETSTANDARD)
using System.Text.Json.Serialization;
#endif

namespace Pug.HttpApiClient.OAuth2
{
	public record OpenIdConfiguration
	{
		[DataMember(Name = "jwks_uri")]
		[JsonProperty("jwks_uri")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName( "jwks_uri")]
#endif
		public string JwksUri { get; set; }
		
		[DataMember(Name="authorization_endpoint")]
		[JsonProperty("authorization_endpoint")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("authorization_endpoint")]
#endif
		public string AuthorizationEndpoint { get; set; }
		
		[DataMember(Name = "token_endpoint")]
		[JsonProperty("token_endpoint")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName( "token_endpoint")]
#endif
		public string TokenEndpoint { get; set; }
		
		[DataMember(Name="userinfo_endpoint")]
		[JsonProperty("userinfo_endpoint")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("userinfo_endpoint")]
#endif
		public string UserInfoEndpoint { get; set; }
		
		[DataMember(Name="end_session_endpoint")]
		[JsonProperty("end_session_endpoint")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("end_session_endpoint")]
#endif
		public string EndSessionEndpoint { get; set; }
		
		[DataMember(Name="check_session_iframe")]
		[JsonProperty("check_session_iframe")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("check_session_iframe")]
#endif
		public string CheckSessionIFrameEndpoint { get; set; }
		
		[DataMember(Name = "revocation_endpoint")]
		[JsonProperty("revocation_endpoint")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("revocation_endpoint")]
#endif
		public string RevocationEndpoint { get; set; }
		
		[DataMember(Name="introspection_endpoint")]
		[JsonProperty("introspection_endpoint")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("introspection_endpoint")]
#endif
		public string IntrospectionEndpoint { get; set; }
		
		[DataMember(Name="device_authorization_endpoint")]
		[JsonProperty("device_authorization_endpoint")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("device_authorization_endpoint")]
#endif
		public string DeviceAuthorizationEndpoint { get; set; }
		
		[DataMember(Name="frontchannel_logout_supported")]
		[JsonProperty("frontchannel_logout_supported")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("frontchannel_logout_supported")]
#endif
		public bool FrontChannelLogoutSupported { get; set; }
		
		[DataMember(Name="frontchannel_logout_session_supported")]
		[JsonProperty("frontchannel_logout_session_supported")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("frontchannel_logout_session_supported")]
#endif
		public bool FrontChannelLogoutSessionSupported { get; set; }
		
		[DataMember(Name="backchannel_logout_supported")]
		[JsonProperty("backchannel_logout_supported")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("backchannel_logout_supported")]
#endif
		public bool BackChannelLogoutSupported { get; set; }
		
		[DataMember(Name="backchannel_logout_session_supported")]
		[JsonProperty("backchannel_logout_session_supported")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("backchannel_logout_session_supported")]
#endif
		public bool BackChannelLogoutSessionSupported { get; set; }
		
		[DataMember(Name="scopes_supported")]
		[JsonProperty("scopes_supported")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("scopes_supported")]
#endif
		public string[] SupportedScope { get; set; }
		
		[DataMember(Name="claims_supported")]
		[JsonProperty("claims_supported")]
#if !(NETCOREAPP2_1 || NETSTANDARD)
		[JsonPropertyName("claims_supported")]
#endif
		public string[] SupportedClaims { get; set; }
	}
}