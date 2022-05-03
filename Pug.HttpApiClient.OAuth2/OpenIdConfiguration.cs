using System.Runtime.Serialization;

namespace Pug.HttpApiClient.OAuth2Decorators
{
	public record OpenIdConfiguration
	{
		[DataMember(Name = "jwks_uri")]
		public string JwksUri { get; set; }
		
		[DataMember(Name="authorization_endpoint")]
		public string AuthorizationEndpoint { get; set; }
		
		[DataMember(Name = "token_endpoint")]
		public string TokenEnndpoint { get; set; }
		
		[DataMember(Name="userinfo_endpoint")]
		public string UserInfoEndpoint { get; set; }
		
		[DataMember(Name="end_session_endpoint")]
		public string EndSessionEndpoint { get; set; }
		
		[DataMember(Name="check_session_iframe")]
		public string CheckSessionIFrameEndpoint { get; set; }
		
		[DataMember(Name = "revocation_endpoint")]
		public string RevocationEndpoint { get; set; }
		
		[DataMember(Name="introspection_endpoint")]
		public string IntrospectionEndpoint { get; set; }
		
		[DataMember(Name="device_authorization_endpoint")]
		public string DeviceAuthorizationEndpoint { get; set; }
		
		[DataMember(Name="frontchannel_logout_supported")]
		public bool FrontChannelLogoutSupported { get; set; }
		
		[DataMember(Name="frontchannel_logout_session_supported")]
		public bool FrontChannelLogoutSessionSupported { get; set; }
		
		[DataMember(Name="backchannel_logout_supported")]
		public bool BackChannelLogoutSupported { get; set; }
		
		[DataMember(Name="backchannel_logout_session_supported")]
		public bool BackChannelLogoutSessionSupported { get; set; }
		
		[DataMember(Name="scopes_supported")]
		public string[] SupportedScope { get; set; }
	}
}