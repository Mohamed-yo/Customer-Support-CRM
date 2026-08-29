namespace CustomerSupportCrm.Api.Configuration;

// Value shape of the "reminder_lead_hrs" RuntimeSetting - how far ahead of a TicketTask's due
// date the reminder background scanner (Story 15 Phase 5) creates a "TaskReminder" notification.
public class ReminderLeadTimeSetting
{
    public double Hours { get; set; } = 24;
}
