using ImageHub.Domain.Common;

namespace ImageHub.Domain.Errors;

public static class ImageErrors
{
    public static Error InvalidHeight(int resizeHeight) => Error.Validation(
        "Image.File.InvalidHeight", $"Image file resize height '{resizeHeight}' is invalid");

    public static Error RecordNotFound(Guid id) => Error.NotFound(
        "Image.Record.NotFound", $"Image record with Id '{id}' not found");

    public static Error FileNotFound(Guid id, int height) => Error.NotFound(
        "Image.File.NotFound", $"Image file with Id '{id}' and height '{height}' not found");
    
    public static Error FileNotFound(string key) => Error.NotFound(
        "Image.File.NotFound", $"Image file with key '{key}' not found");
    
    public static Error RecordRetrievalFailed(Guid id) => Error.Failure(
        "Image.Record.RetrievalFailed", $"Image record with Id '{id}' retrieval failed");

    public static readonly Error RecordSaveFailed = Error.Failure(
        "Image.Record.CreationFailed", "Image record save failed");

    public static readonly Error FileUploadFailed = Error.Failure(
        "Image.File.UploadFailed", "Image file upload failed");

    public static Error RecordDeletionFailed(Guid id) => Error.Failure(
        "Image.Record.DeletionFailed", $"Image record with Id '{id}' deletion failed");

    public static Error FileDeletionFailed(string key) => Error.Failure(
        "Image.File.DeletionFailed", $"Image file with key '{key}' deletion failed");
    
    public static Error FileRetrievalFailed(string key) => Error.Failure(
        "Image.File.RetrievalFailed", $"Image file with key '{key}' retrieval failed");

    public static Error PreSignedUrlGenerationFailed(string key) => Error.Failure(
        "Image.File.PreSignedUrlGenerationFailed", $"Image file with key '{key}' pre signed url generation failed");
    
    public static Error MessageEnqueueFailed(Guid id) => Error.Failure(
        "Image.Message.EnqueueFailed", $"Message with image Id '{id}' queue enqueue failed");
    
    public static readonly Error MessageRetrievalFailed = Error.Failure(
        "Image.Message.RetrievalFailed", "Message queue retrieval failed");
    
    public static readonly Error MessageDeletionFailed = Error.Failure(
        "Image.Message.DeletionFailed", "Message queue deletion failed");
}