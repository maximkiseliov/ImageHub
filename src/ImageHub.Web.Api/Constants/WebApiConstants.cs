namespace ImageHub.Web.Api.Constants;

public static class WebApiConstants
{
    public static class ApiEndpoints
    {
        private const string ApiBase = "api";

        public static class Images
        {
            private const string Base = $"{ApiBase}/images";

            public const string Upload = Base;
            public const string Get = $"{Base}/{{id:guid}}";
            public const string Resize = $"{Base}/{{id:guid}}/resize";
            public const string Delete = $"{Base}/{{id:guid}}";
        }
    }

    public static class Scalar
    {
        public const string DocumentName = "v1";
        public const string Title = "Scalar UI";
    }

    public const long MaxFileSize = 300 * 1024 * 1024;
}