namespace Web.Extensions
{
    public static class ApplicationPathResolver
    {
        public static string GetPathBase(IConfiguration configuration, HttpRequest? request = null)
        {
            if (request?.PathBase.HasValue == true)
            {
                var requestPathBase = NormalizePathBase(request.PathBase.Value);
                if (!string.IsNullOrEmpty(requestPathBase))
                    return requestPathBase;
            }

            var configuredPathBase = configuration["PathBase"];
            if (!string.IsNullOrWhiteSpace(configuredPathBase))
                return NormalizePathBase(configuredPathBase);

            if (Uri.TryCreate(configuration["WebUrl"], UriKind.Absolute, out var webUri))
                return NormalizePathBase(webUri.AbsolutePath);

            return string.Empty;
        }

        public static string GetBaseHref(IConfiguration configuration, HttpRequest? request = null)
        {
            var pathBase = GetPathBase(configuration, request);
            return string.IsNullOrEmpty(pathBase) ? "/" : $"{pathBase}/";
        }

        public static string CombineWithPathBase(
            IConfiguration configuration,
            HttpRequest request,
            string localPath)
        {
            var pathBase = GetPathBase(configuration, request);
            var normalizedLocalPath = string.IsNullOrWhiteSpace(localPath)
                ? "/"
                : $"/{localPath.TrimStart('/')}";

            return $"{pathBase}{normalizedLocalPath}";
        }

        private static string NormalizePathBase(string? pathBase)
        {
            if (string.IsNullOrWhiteSpace(pathBase) || pathBase == "/")
                return string.Empty;

            return $"/{pathBase.Trim().Trim('/')}";
        }
    }
}
