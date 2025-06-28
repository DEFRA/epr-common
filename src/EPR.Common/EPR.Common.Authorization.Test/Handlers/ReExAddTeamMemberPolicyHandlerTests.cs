using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Handlers;
using EPR.Common.Authorization.Requirements;
using EPR.Common.Authorization.Test.TestClasses;

namespace EPR.Common.Authorization.Test.Handlers;

[TestClass]
public class ReExAddTeamMemberPolicyHandlerTests : PolicyHandlerTestsBase<ReExAddTeamMemberPolicyHandler<MySession>,
	ReExAddTeamMemberRequirement, MySession>
{
	[TestInitialize]
	public void Initialise() => SetUp();

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExApprovedPerson, RoleInOrganisation.Admin, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_Succeeds_WhenUserHasRolesInClaim_AndIsEnrolledAdmin(
		string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
		await HandleRequirementAsync_Succeeds_WhenUserHasRolesInClaim(serviceRoleKey, roleInOrganisation, enrolmentStatus);

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExAdminUser, RoleInOrganisation.Employee, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExApprovedPerson, RoleInOrganisation.Employee, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_Succeeds_WhenUserHasRolesInClaim_AndIsEnrolledBasic(
	string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
	await HandleRequirementAsync_Succeeds_WhenUserHasRolesInClaim(serviceRoleKey, roleInOrganisation, enrolmentStatus);

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExDelegatedPerson, RoleInOrganisation.NotSet, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExStandardUser, RoleInOrganisation.Admin, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExBasicUser, RoleInOrganisation.NotSet, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_Fails_WhenUserIsNotAuthenticated(string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
		await HandleRequirementAsync_Fails_WhenUserIsNotAuthenticated(serviceRoleKey, roleInOrganisation, enrolmentStatus);

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExBasicUser, RoleInOrganisation.NotSet, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExStandardUser, RoleInOrganisation.NotSet, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_IsNotAuthorised_WhenUserDataExistsInClaimButUserRoleIsNotValid(string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
		await HandleRequirementAsync_Fails_WhenUserDataExistsInClaimButUserRoleIsNotAuthorised(serviceRoleKey, roleInOrganisation, enrolmentStatus);

	[TestMethod]
	public async Task ReExAddTeamMember_IsNotAuthorised_WhenAuthorizationHandlerContextResourceDoesNotExist() =>
		await HandleRequirementAsync_Fails_WhenAuthorizationHandlerContextResourceDoesNotExist();

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExApprovedPerson, RoleInOrganisation.Admin, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExAdminUser, RoleInOrganisation.Admin, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_IsAuthorised_WhenCacheContainRequiredUserData(string serviceRoleKey,
		string roleInOrganisation, string enrolmentStatus)
	{
		await HandleRequirementAsync_Succeeds_WhenCacheContainRequiredUserData(serviceRoleKey, roleInOrganisation, enrolmentStatus);
		HttpResponseMock.VerifyNoOtherCalls();
	}

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExAdminUser, RoleInOrganisation.Admin, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExApprovedPerson, RoleInOrganisation.Admin, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_IsAuthorised_WhenCacheContainRequiredUserData_And_RedirectIsSpecified(
		string serviceRoleKey, string roleInOrganisation, string enrolmentStatus)
	{
		SetupSignInRedirect("/manage-account/reex");
		await HandleRequirementAsync_Succeeds_WhenCacheContainRequiredUserData(serviceRoleKey, roleInOrganisation, enrolmentStatus);
		HttpResponseMock.Verify(response => response.Redirect("/manage-account/reex"));
	}

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExStandardUser, RoleInOrganisation.Admin, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_IsNotAuthorised_WhenUserDataExistsInCacheButUserRoleIsNotValid(string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
		await HandleRequirementAsync_Fails_WhenUserDataExistsInCacheButUserRoleIsNotAuthorised(serviceRoleKey, roleInOrganisation, enrolmentStatus);

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExAdminUser, RoleInOrganisation.Admin, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExApprovedPerson, RoleInOrganisation.Admin, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_IsAuthorised_WhenClaimsComeFromFromApi(string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
		await HandleRequirementAsync_Succeeds_WhenUserDataIsRetrievedFromApi(serviceRoleKey, roleInOrganisation, enrolmentStatus);

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExBasicUser, RoleInOrganisation.NotSet, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExStandardUser, RoleInOrganisation.Employee, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_IsNotAuthorised_WhenUserDataIsRetrievedFromApiButUserRoleIsNotValid(string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
		await HandleRequirementAsync_Fails_WhenUserDataIsRetrievedFromApiButUserRoleIsNotAuthorised(serviceRoleKey, roleInOrganisation, enrolmentStatus);

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExBasicUser, RoleInOrganisation.NotSet, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExStandardUser, RoleInOrganisation.Employee, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_IsNotAuthorised_WhenUserDataIsRetrievedFromApiWithOutUserOrganisations(string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
		await HandleRequirementAsync_Fails_WhenUserOrganisations_IsEmpty(serviceRoleKey, roleInOrganisation, enrolmentStatus);
	
	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExBasicUser, RoleInOrganisation.NotSet, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExStandardUser, RoleInOrganisation.Employee, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_IsNotAuthorised_WhenUserDataIsRetrievedFromApiWithMultipleOrganisations(string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
		await HandleRequirementAsync_Fails_WhenUserOrganisations_IsMoreThanOne(serviceRoleKey, roleInOrganisation, enrolmentStatus);

	[TestMethod]
	[DataRow(ServiceRoleKeys.ReExBasicUser, RoleInOrganisation.NotSet, EnrolmentStatuses.Enrolled)]
	[DataRow(ServiceRoleKeys.ReExStandardUser, RoleInOrganisation.Employee, EnrolmentStatuses.Enrolled)]
	public async Task ReExAddTeamMember_IsNotAuthorised_WhenUserDataIsRetrievedFromApiWithOutOrganisationEnrolments(string serviceRoleKey, string roleInOrganisation, string enrolmentStatus) =>
		await HandleRequirementAsync_Fails_WhenOrganisationEnrolments_IsEmpty(serviceRoleKey, roleInOrganisation, enrolmentStatus);

	[TestMethod]
	public async Task ReExAddTeamMember_IsNotAuthorised_WhenApiCallFails() =>
		await HandleRequirementAsync_Fails_WhenApiCallFails();
}
