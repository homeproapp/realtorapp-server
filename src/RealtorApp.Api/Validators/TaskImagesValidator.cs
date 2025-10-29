using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace RealtorApp.Api.Validators;

public class TaskImagesValidator : AbstractValidator<IFormFile[]>
{
    public TaskImagesValidator()
    {
        RuleFor(x => x.Length)
            .LessThanOrEqualTo(20)
            .WithMessage("Maximum 20 images allowed");

        RuleForEach(x => x)
            .SetValidator(new TaskImageValidator());
    }
}

public class TaskImageValidator : AbstractValidator<IFormFile>
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/jpg", "image/png", "image/webp" };
    private const long MaxSizeBytes = 10 * 1024 * 1024;

    public TaskImageValidator()
    {
        RuleFor(x => x.Length)
            .LessThanOrEqualTo(MaxSizeBytes)
            .WithMessage(x => $"Image '{x.FileName}' exceeds 10 MB limit");

        RuleFor(x => x.ContentType)
            .Must(ct => AllowedTypes.Contains(ct.ToLower()))
            .WithMessage(x => $"Invalid image type for '{x.FileName}'. Only JPEG, PNG, and WebP allowed");
    }
}
