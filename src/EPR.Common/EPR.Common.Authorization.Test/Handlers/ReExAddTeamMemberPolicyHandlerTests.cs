namespace EPR.Common.Authorization.Test.Handlers;

using Authorization.Handlers;
using Authorization.Requirements;
using EPR.Common.Authorization.Constants;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using TestClasses;

[TestClass]
public class ReExAddTeamMemberPolicyHandlerTests : ReExPolicyHandlerTestFixture<
    ReExAddTeamMemberPolicyHandler<MySession>,
    ReExAddTeamMemberRequirement,
    MySession>
{
    [TestInitialize]
    public void TestInitialize()
    {
        base.SetUp();
    }

    [TestMethod]
    public async Task HandleRequirementAsync_Succeeds_WhenUserHasReExAdminUserRoleInClaim()
    {
        await HandleRequirementAsync_Succeeds_WhenUserHasAllowedRoleInClaimWithOrganisation(
            ServiceRoleKeys.ReExAdminUser,
			RoleInOrganisation.Admin,
			EnrolmentStatuses.Enrolled,
			_testOrganisationId);
    }

    [TestMethod]
    public async Task HandleRequirementAsync_Succeeds_WhenUserHasReExApprovedPersonRoleInClaim()
    {
        await HandleRequirementAsync_Succeeds_WhenUserHasAllowedRoleInClaimWithOrganisation(
            ServiceRoleKeys.ReExApprovedPerson,
			RoleInOrganisation.Employee,
			EnrolmentStatuses.Enrolled,
			_testOrganisationId);
    }

    // --- Tests for failed authorization based on claims ---

    [TestMethod]
    public async Task HandleRequirementAsync_Fails_WhenUserHasDisallowedRoleInClaim()
    {
        await HandleRequirementAsync_Fails_WhenUserDataExistsInClaimButUserRoleIsNotAuthorisedWithOrganisation(
            ServiceRoleKeys.ReExBasicUser, // This role is NOT allowed for AddTeamMember
			RoleInOrganisation.Employee,
			EnrolmentStatuses.Enrolled,
			_testOrganisationId);
    }

    [TestMethod]
    public async Task HandleRequirementAsync_Fails_WhenUserHasNoUserDataClaim()
    {
        await HandleRequirementAsync_Fails_WhenUserDataClaimIsMissing();
    }

    [TestMethod]
    public async Task HandleRequirementAsync_Succeeds_WhenCacheContainsReExAdminUserRole()
    {
        await HandleRequirementAsync_Succeeds_WhenCacheContainRequiredUserDataWithOrganisation(
            ServiceRoleKeys.ReExAdminUser,
			RoleInOrganisation.Admin,
			EnrolmentStatuses.Enrolled,
			_testOrganisationId);
    }

    [TestMethod]
    public async Task HandleRequirementAsync_Fails_WhenCacheContainsDisallowedRole()
    {
        await HandleRequirementAsync_Fails_WhenUserDataExistsInCacheButUserRoleIsNotAuthorisedWithOrganisation(
            "SomeOtherRole",
			RoleInOrganisation.Admin,
			EnrolmentStatuses.Enrolled,
			_testOrganisationId);
    }

    [TestMethod]
    public async Task HandleRequirementAsync_Succeeds_WhenApiRetrievesReExAdminUserRole()
    {
        await HandleRequirementAsync_Succeeds_WhenUserDataIsRetrievedFromApiWithOrganisation(
            ServiceRoleKeys.ReExAdminUser,
			RoleInOrganisation.Admin,
			EnrolmentStatuses.Enrolled,
			_testOrganisationId);
    }

    [TestMethod]
    public async Task HandleRequirementAsync_Fails_WhenApiRetrievesDisallowedRole()
    {
        await HandleRequirementAsync_Fails_WhenUserDataIsRetrievedFromApiButUserRoleIsNotAuthorisedWithOrganisation(
            "SomeOtherRole",
			RoleInOrganisation.Employee,
			EnrolmentStatuses.Enrolled,
			_testOrganisationId);
    }
    
    [TestMethod]
    public async Task HandleRequirementAsync_Fails_WhenNoOrganisationIdFoundInSessionOrRoute()
    {
        await base.HandleRequirementAsync_Fails_WhenNoOrganisationIdInSessionOrRoute();
    }

    [TestMethod]
    public async Task HandleRequirementAsync_Succeeds_WhenOrganisationIdAvailableFromRoute()
    {
        await base.HandleRequirementAsync_Succeeds_WhenOrganisationIdFromRoute();
    }
}