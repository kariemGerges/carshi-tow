namespace CarshiTow.Api.Srs;

public static class SrsApiEnvelopePaths
{
    public static bool ShouldApply(PathString path)
    {
        if (!path.StartsWithSegments("/api/v1"))
        {
            return false;
        }

        if (path.StartsWithSegments("/api/v1/webhooks"))
        {
            return false;
        }

        return true;
    }
}
