namespace CustomerSupportCrm.Api.Configuration;

// Allow-list of RuntimeSetting.Key values, mirroring TicketsController's
// AllowedStatuses/Categories/Priorities pattern for controller-enforced string constants.
internal static class RuntimeSettingKeys
{
    public const string SlaTargets = "sla_targets";
    public const string ReminderLeadHrs = "reminder_lead_hrs";
    public const string Branding = "branding";

    public static readonly string[] All = { SlaTargets, ReminderLeadHrs, Branding };
}
