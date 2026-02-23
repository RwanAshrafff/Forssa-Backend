namespace Forssa.Application.DTOs;

public class CompletedCourseCertificateDTO
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}

public class CertificateDocumentDTO
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public class CertificateTemplateInputDTO
{
    public string? StudentFullName { get; set; }
    public string? InstructorFullName { get; set; }
    public string? InstructorPosition { get; set; }
    public string? CourseTitle { get; set; }
}
