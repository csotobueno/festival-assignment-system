namespace Festival.Domain.Assignments;

public sealed record AssignmentRequestRejection
{
    public string Code { get; }

    public string Message { get; }

    private AssignmentRequestRejection(
        string code,
        string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Rejection code cannot be empty.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Rejection message cannot be empty.",
                nameof(message));
        }

        Code = code.Trim().ToUpperInvariant();
        Message = message.Trim();
    }

    public static AssignmentRequestRejection Create(
        string? code,
        string? message)
    {
        return new AssignmentRequestRejection(code!, message!);
    }
}
