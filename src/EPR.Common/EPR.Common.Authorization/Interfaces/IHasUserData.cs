namespace EPR.Common.Authorization.Interfaces;

using Models;

public interface IHasUserData
{
    public UserData UserData { get; set; }
	public Guid? SelectedOrganisationId
	{
		get => null;
		set { }
	}
}