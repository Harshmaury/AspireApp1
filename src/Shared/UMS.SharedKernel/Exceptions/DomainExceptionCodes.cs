namespace UMS.SharedKernel.Exceptions;

public static class DomainExceptionCodes
{
    // NOT_FOUND codes ? 404
    public static readonly HashSet<string> NotFound =
    [
        "NOT_FOUND", "ROOM_NOT_FOUND", "HOSTEL_NOT_FOUND",
        "ALLOTMENT_NOT_FOUND", "COMPLAINT_NOT_FOUND", "DEPT_NOT_FOUND"
    ];

    // CONFLICT codes ? 409
    public static readonly HashSet<string> Conflict =
    [
        "DUPLICATE_CODE", "DUPLICATE_EMPLOYEE_ID", "DUPLICATE_ASSIGNMENT",
        "ROOM_EXISTS", "ALREADY_ALLOTTED", "ALREADY_INACTIVE",
        "ALREADY_ACTIVE", "ALREADY_PUBLISHED", "SAME_STATUS"
    ];
}
