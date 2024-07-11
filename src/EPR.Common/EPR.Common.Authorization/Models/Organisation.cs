namespace EPR.Common.Authorization.Models;

public class Organisation
{
    public Guid? Id { get; set; }

    public string? Name { get; set; }

    public string? OrganisationRole { get; set; }

    public string? OrganisationType { get; set; }
    
    public string? OrganisationNumber { get; set; }

    public int? NationId { get; set; }

    public string OrgAddress { get; set; }

    public string? JobTitle { get; set; }
}