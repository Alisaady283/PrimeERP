namespace PrimeERP.Api.Features.Builder
{
    public sealed record FlagBody(bool Value);

    public sealed record VersionBody(string Version);

    public sealed record EditionBody(List<string> Keys);
}
