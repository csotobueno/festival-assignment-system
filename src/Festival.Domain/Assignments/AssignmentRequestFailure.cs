namespace Festival.Domain.Assignments;

public sealed record AssignmentRequestFailure
{
    public string Code { get; }

    public string Message { get; }

    private AssignmentRequestFailure(
        string code,
        string message)
    {
        Code = code;
        Message = message;
    }

    public static AssignmentRequestFailure Create(
        string? code,
        string? message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Failure code cannot be empty.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Failure message cannot be empty.",
                nameof(message));
        }

        return new AssignmentRequestFailure(
            code.Trim().ToUpperInvariant(),
            message.Trim());
    }
}