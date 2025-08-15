using EduShield.Core.Dtos;
using FluentValidation;

namespace EduShield.Core.Validators;

public class CreatePerformanceReqValidator : AbstractValidator<CreatePerformanceReq>
{
    public CreatePerformanceReqValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required");

        RuleFor(x => x.FacultyId)
            .NotEmpty()
            .WithMessage("Faculty ID is required");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required")
            .MaximumLength(100)
            .WithMessage("Subject must not exceed 100 characters");

        RuleFor(x => x.Marks)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Marks must be greater than or equal to 0");

        RuleFor(x => x.MaxMarks)
            .GreaterThan(0)
            .WithMessage("Maximum marks must be greater than 0");

        RuleFor(x => x.Marks)
            .LessThanOrEqualTo(x => x.MaxMarks)
            .WithMessage("Marks cannot exceed maximum marks");

        RuleFor(x => x.ExamDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Exam date cannot be in the future");
    }
}