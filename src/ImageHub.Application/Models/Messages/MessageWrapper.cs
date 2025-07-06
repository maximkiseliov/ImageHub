namespace ImageHub.Application.Models.Messages;

public sealed class MessageWrapper<T> where T : class
{
    public T? Body { get; init; }
    public required string MessageId { get; init; }
}