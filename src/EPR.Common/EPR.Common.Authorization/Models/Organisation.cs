namespace EPR.Common.Authorization.Models;

public class Organisation
{
    public Guid? Id { get; set; }

    public string? Name { get; set; }

    public string? OrganisationRole { get; set; }

    public string? OrganisationType { get; set; }
    
    public string? OrganisationNumber { get; set; }
}