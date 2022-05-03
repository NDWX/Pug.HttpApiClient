using System;

namespace EzySend.Integration.IntegratedCarriers.AusPost
{
	public class ErrorResponse : Exception
	{
		public Error[] Errors { get; set; }
	}
}
