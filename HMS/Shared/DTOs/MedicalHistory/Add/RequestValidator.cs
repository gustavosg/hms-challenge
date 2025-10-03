using FluentValidation;

namespace Shared.DTOs.MedicalHistory.Add;

public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("Patient ID is required");

        RuleFor(x => x.Document)
            .NotEmpty()
            .WithMessage("Patient document is required")
            .Length(11, 14)
            .WithMessage("Document must be between 11 and 14 characters")
            .Matches(@"^\d+$")
            .WithMessage("Document must contain only numbers");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters");

        RuleFor(x => x.Diagnosis)
            .SetValidator(new DiagnosisRequestValidator())
            .When(x => x.Diagnosis != null);

        RuleFor(x => x.Exam)
            .SetValidator(new ExamRequestValidator())
            .When(x => x.Exam != null);

        RuleFor(x => x.Prescription)
            .SetValidator(new PrescriptionRequestValidator())
            .When(x => x.Prescription != null);
    }
}

public class DiagnosisRequestValidator : AbstractValidator<DiagnosisRequest>
{
    public DiagnosisRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Diagnosis description is required")
            .MaximumLength(500)
            .WithMessage("Diagnosis description cannot exceed 500 characters");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Diagnosis date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Diagnosis date cannot be in the future");
    }
}

public class ExamRequestValidator : AbstractValidator<ExamRequest>
{
    public ExamRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Exam type is required")
            .MaximumLength(100)
            .WithMessage("Exam type cannot exceed 100 characters");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Exam date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Exam date cannot be in the future");

        RuleFor(x => x.Result)
            .MaximumLength(2000)
            .WithMessage("Result cannot exceed 2000 characters");
    }
}

public class PrescriptionRequestValidator : AbstractValidator<PrescriptionRequest>
{
    public PrescriptionRequestValidator()
    {
        RuleFor(x => x.Medication)
            .NotEmpty()
            .WithMessage("Medication is required")
            .MaximumLength(200)
            .WithMessage("Medication cannot exceed 200 characters");

        RuleFor(x => x.Dosage)
            .NotEmpty()
            .WithMessage("Dosage is required")
            .MaximumLength(100)
            .WithMessage("Dosage cannot exceed 100 characters");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Prescription date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Prescription date cannot be in the future");
    }
}