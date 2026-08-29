namespace CustomerSupportCrm.Api.Configuration;

// One entry of the "sla_targets" RuntimeSetting, JSON-serialized as
// Dictionary<string, SlaTargetSetting> keyed by ticket priority ("Urgent"/"High"/"Normal"/"Low").
public class SlaTargetSetting
{
    public double ResponseHours { get; set; }
    public double ResolutionHours { get; set; }
}
