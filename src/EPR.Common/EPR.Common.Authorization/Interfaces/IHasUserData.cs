namespace EPR.Common.Authorization.Interfaces;

using Models;

public interface IHasUserData
{
    public UserData UserData { get; set; }

	/// <summary>
	/// Optional property to store the selected organisation ID.
	/// Provided with a default implementation to maintain backward compatibility with existing implementations.
	/// Will return <c>null</c> unless explicitly overridden by the implementing service.
	/// </summary>
	public Guid? SelectedOrganisationId
	{
		get => null;
		set { }
	}
}