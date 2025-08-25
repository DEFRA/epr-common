namespace EPR.Common.Authorization.Test.TestClasses;

using Interfaces;
using Models;

public class MySession : IHasUserData
{
    public MySession()
    {
    }

    public UserData UserData { get; set; } = new();
    
    public Guid? SelectedOrganisationId { get; set; }
}