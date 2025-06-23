namespace EPR.Common.Authorization.Models;

public class Enrolment
{
	public int? EnrolmentId { get; set; }

	public string? EnrolmentStatus { get; set; }

	public string? ServiceRole { get; set; }

	public string? Service { get; set; }

	public int? ServiceRoleId { get; set; }
}