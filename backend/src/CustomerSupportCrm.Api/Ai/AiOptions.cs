namespace CustomerSupportCrm.Api.Ai;

// Peer to Integrations/IntegrationOptions.cs's option classes. Blank in the committed
// appsettings.json (see Jwt:SigningKey precedent) - real values only in
// appsettings.Development.json, and only ever needed for a real vendor provider (never for
// "none" or "mock").
public sealed class AiOptions
{
    // "none" (default - NullAiProvider) | "mock" (MockAiProvider, deterministic canned
    // output, no network call) | a real vendor name once one is wired up.
    public string Provider { get; set; } = "none";
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
}
