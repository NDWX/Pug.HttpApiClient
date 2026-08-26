namespace Pug.HttpApiClient.Tests.Json
{
	public class Widget
	{
		public string Name { get; set; }

		public int Quantity { get; set; }
	}

	public class WidgetOrder
	{
		public string Reference { get; set; }

		public int LineCount { get; set; }
	}
}
