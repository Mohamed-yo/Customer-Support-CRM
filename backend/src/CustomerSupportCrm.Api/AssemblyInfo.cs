using System.Runtime.CompilerServices;

// Story 12: lets the test project exercise internal helpers (e.g. ChannelIntakeHelpers)
// directly, without exposing them as public API.
[assembly: InternalsVisibleTo("CustomerSupportCrm.Api.Tests")]
