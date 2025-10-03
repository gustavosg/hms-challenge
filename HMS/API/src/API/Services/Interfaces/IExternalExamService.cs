using Shared.DTOs.ExternalExam;

namespace API.Services.Interfaces;

public interface IExternalExamService
{
    Task<IEnumerable<Response>> GetExternalExamsAsync(Request request);
    Task<Response?> GetExternalExamByIdAsync(string examId);
}