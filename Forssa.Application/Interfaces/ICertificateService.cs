using Forssa.Application.DTOs;

namespace Forssa.Application.Interfaces;

public interface ICertificateService
{
    Task<IReadOnlyList<CompletedCourseCertificateDTO>> GetCompletedCoursesAsync(string supabaseId);
    Task<CertificateDocumentDTO> GenerateCourseCertificateAsync(string supabaseId, Guid courseId, CertificateTemplateInputDTO? input = null);
}
