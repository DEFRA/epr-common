namespace EPR.Common.Sample.Data.Tables;

using Functions.Database.Entities;
using Functions.Database.Entities.Interfaces;
using System.Text.Json.Serialization;

public class OrgUser
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    public bool PrivacyPolicyAccepted { get; set; }

    public DateTime PrivacyPolicyAcceptedDateTime { get; set; }

    public bool DeclarationAccepted { get; set; }

    public DateTime DeclarationAcceptedDateTime { get; set; }
}