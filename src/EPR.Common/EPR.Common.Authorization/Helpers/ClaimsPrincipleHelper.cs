namespace EPR.Common.Authorization.Helpers;

using Constants;
using Models;
using System.Security.Claims;
using System.Text.Json;

public static class ClaimsPrincipleHelper
{
    public static bool IsApprovedOrDelegatedPerson(ClaimsPrincipal claimsPrinciple)
    {
        var userDataClaim = claimsPrinciple.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);

        if (userDataClaim == null)
        {
            return false;
        }

        var userData = JsonSerializer.Deserialize<UserData>(userDataClaim.Value);

        return userData?.ServiceRole is ServiceRoles.ApprovedPerson or ServiceRoles.DelegatedPerson;
    }

    public static bool IsApprovedDelegatedOrBasicPerson(ClaimsPrincipal claimsPrinciple)
    {
        var userDataClaim = claimsPrinciple.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);

        if (userDataClaim == null)
        {
            return false;
        }

        var userData = JsonSerializer.Deserialize<UserData>(userDataClaim.Value);

        return userData?.ServiceRole is ServiceRoles.ApprovedPerson or ServiceRoles.DelegatedPerson or ServiceRoles.BasicUser;
    }

    public static bool CanUploadFilesForOrganisation(ClaimsPrincipal claimsPrinciple)
    {
        var userDataClaim = claimsPrinciple.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);

        if (userDataClaim == null)
        {
            return false;
        }

        var userData = JsonSerializer.Deserialize<UserData>(userDataClaim.Value);

        var isServiceRoleAllowed = userData?.ServiceRole is ServiceRoles.ApprovedPerson
            or ServiceRoles.DelegatedPerson
            or ServiceRoles.BasicUser;

        var isPersonRoleAllowed = userData?.RoleInOrganisation is RoleInOrganisation.Admin;

        return (isServiceRoleAllowed || isPersonRoleAllowed) && userData?.ServiceRole is not ServiceRoles.RegulatorBasic &&
               userData?.ServiceRole is not ServiceRoles.RegulatorAdmin;
    }

    public static bool IsEnrolledAdmin(ClaimsPrincipal claimsPrinciple)
    {
        var userDataClaim = claimsPrinciple.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);

        if (userDataClaim == null)
        {
            return false;
        }

        var userData = JsonSerializer.Deserialize<UserData>(userDataClaim.Value);

        return userData?.RoleInOrganisation == RoleInOrganisation.Admin &&
               (userData.EnrolmentStatus == EnrolmentStatuses.Approved
                || userData.EnrolmentStatus == EnrolmentStatuses.Enrolled
                || userData.EnrolmentStatus == EnrolmentStatuses.Pending);
    }

    public static bool IsRegulator(ClaimsPrincipal claimsPrinciple)
    {
        var userDataClaim = claimsPrinciple.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);

        if (userDataClaim == null)
        {
            return false;
        }

        var userData = JsonSerializer.Deserialize<UserData>(userDataClaim.Value);

        return userData?.ServiceRole is ServiceRoles.RegulatorBasic || userData?.ServiceRole is ServiceRoles.RegulatorAdmin &&
               (userData.EnrolmentStatus == EnrolmentStatuses.Approved
                || userData.EnrolmentStatus == EnrolmentStatuses.Enrolled
                || userData.EnrolmentStatus == EnrolmentStatuses.Pending);
    }

    public static bool IsRegulatorAdmin(ClaimsPrincipal claimsPrinciple)
    {
        var userDataClaim = claimsPrinciple.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);

        if (userDataClaim == null)
        {
            return false;
        }

        var userData = JsonSerializer.Deserialize<UserData>(userDataClaim.Value);

        return userData?.ServiceRole is ServiceRoles.RegulatorAdmin;
    }

    public static bool IsEnrolledAdminOrBasic(ClaimsPrincipal claimsPrinciple)
    {
        var userDataClaim = claimsPrinciple.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);

        if (userDataClaim == null)
        {
            return false;
        }
        var userData = JsonSerializer.Deserialize<UserData>(userDataClaim.Value);
        return ((userData?.ServiceRole is ServiceRoles.BasicUser && userData?.RoleInOrganisation == RoleInOrganisation.Employee )|| userData?.RoleInOrganisation == RoleInOrganisation.Admin) &&
               (userData.EnrolmentStatus == EnrolmentStatuses.Approved
                || userData.EnrolmentStatus == EnrolmentStatuses.Enrolled
                || userData.EnrolmentStatus == EnrolmentStatuses.Pending);
	}

	public static bool IsReExAdminOrApprovedPerson(ClaimsPrincipal claimsPrincipal)
	{
		var userDataClaim = claimsPrincipal.FindFirst(ClaimTypes.UserData)?.Value;
		if (string.IsNullOrWhiteSpace(userDataClaim))
		{
			return false;
		}

		UserData? userData;
		try
		{
			userData = JsonSerializer.Deserialize<UserData>(userDataClaim);
		}
		catch (JsonException)
		{
			return false;
		}

		var enrolments = userData?.Organisations?
			.FirstOrDefault()?
			.Enrolments;

		if (enrolments == null || enrolments.Count == 0)
		{
			return false;
		}

		return enrolments.Any(e =>
			e.ServiceRoleKey is ServiceRoleKeys.ReExAdminUser or ServiceRoleKeys.ReExApprovedPerson);
	}

}