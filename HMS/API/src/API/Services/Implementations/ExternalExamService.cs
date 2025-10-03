using API.Services.Interfaces;
using Shared.DTOs.ExternalExam;
using System.Text.Json;

namespace API.Services.Implementations;

public class ExternalExamService : IExternalExamService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalExamService> _logger;

    public ExternalExamService(HttpClient httpClient, ILogger<ExternalExamService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<Response>> GetExternalExamsAsync(Request request)
    {
        try
        {
            _logger.LogInformation("Consulting external exams for document: {Document}", request.PatientDocument);

            // Mock de dados para demonstra��o
            var mockExams = GenerateMockExams(request.PatientDocument, request.ExamType, request.StartDate, request.EndDate);
            
            // Simular delay de API externa
            await Task.Delay(200);
            
            return mockExams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consulting external exams for document: {Document}", request.PatientDocument);
            throw new InvalidOperationException("Failed to retrieve external exams", ex);
        }
    }

    public async Task<Response?> GetExternalExamByIdAsync(string examId)
    {
        try
        {
            _logger.LogInformation("Consulting external exam with ID: {ExamId}", examId);

            var mockExam = GenerateMockExamById(examId);
            
            // Simular delay de API externa
            await Task.Delay(100);
            
            return mockExam;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consulting external exam with ID: {ExamId}", examId);
            throw new InvalidOperationException("Failed to retrieve external exam", ex);
        }
    }

    private static IEnumerable<Response> GenerateMockExams(string patientDocument, string? examType, DateTime? startDate, DateTime? endDate)
    {
        var exams = new List<Response>();
        var random = new Random(patientDocument.GetHashCode());

        var examTypes = new[]
        {
            "Hemograma Completo",
            "Glicemia",
            "Colesterol Total",
            "Triglicer�deos",
            "Ureia",
            "Creatinina",
            "Raio-X T�rax",
            "Ultrassom Abdominal",
            "Eletrocardiograma",
            "Ecocardiograma"
        };

        var laboratories = new[]
        {
            "Lab S�o Marcos",
            "Laborat�rio Central", 
            "Bio An�lises",
            "Lab Vida",
            "Centro de Diagn�sticos"
        };

        int examCount = random.Next(3, 9);
        
        for (int i = 0; i < examCount; i++)
        {
            var selectedExamType = examTypes[random.Next(examTypes.Length)];
            
            if (!string.IsNullOrWhiteSpace(examType) && 
                !selectedExamType.Contains(examType, StringComparison.OrdinalIgnoreCase))
                continue;

            var examDate = DateTime.UtcNow.AddDays(-random.Next(1, 365));
            
            if (startDate.HasValue && examDate < startDate.Value) continue;
            if (endDate.HasValue && examDate > endDate.Value) continue;

            var resultDate = examDate.AddDays(random.Next(1, 5));
            var laboratory = laboratories[random.Next(laboratories.Length)];
            var result = GenerateExamResult(selectedExamType, random);

            exams.Add(new Response(
                Id: $"EXT_{patientDocument}_{i + 1:D3}",
                PatientDocument: patientDocument,
                ExamType: selectedExamType,
                ExamDate: examDate,
                Laboratory: laboratory,
                Status: "Finalizado",
                Result: result
            ));
        }

        return exams.OrderByDescending(e => e.ExamDate);
    }

    private static Response? GenerateMockExamById(string examId)
    {
        if (!examId.StartsWith("EXT_"))
            return null;

        var parts = examId.Split('_');
        if (parts.Length != 3)
            return null;

        var document = parts[1];
        var random = new Random(examId.GetHashCode());

        var examTypes = new[] { "Hemograma Completo", "Glicemia", "Colesterol Total" };
        var selectedExamType = examTypes[random.Next(examTypes.Length)];
        var examDate = DateTime.UtcNow.AddDays(-random.Next(1, 365));
        var result = GenerateExamResult(selectedExamType, random);

        return new Response(
            Id: examId,
            PatientDocument: document,
            ExamType: selectedExamType,
            ExamDate: examDate,
            Laboratory: "Lab S�o Marcos",
            Status: "Finalizado",
            Result: result
        );
    }

    private static ExamResult GenerateExamResult(string examType, Random random)
    {
        var values = new Dictionary<string, object>();
        var observations = "";

        switch (examType)
        {
            case "Hemograma Completo":
                values["Hem�cias"] = $"{random.NextDouble() * (5.5 - 4.0) + 4.0:F2} milh�es/?L";
                values["Hemoglobina"] = $"{random.NextDouble() * (16 - 12) + 12:F1} g/dL";
                values["Hemat�crito"] = $"{random.NextDouble() * (48 - 36) + 36:F1}%";
                values["Leuc�citos"] = $"{random.Next(4000, 11000):N0}/?L";
                observations = "Valores dentro da normalidade";
                break;

            case "Glicemia":
                var glucose = random.Next(70, 200);
                values["Glicose"] = $"{glucose} mg/dL";
                observations = glucose > 100 ? "Glicemia levemente elevada" : "Valor normal";
                break;

            case "Colesterol Total":
                var cholesterol = random.Next(150, 300);
                values["Colesterol Total"] = $"{cholesterol} mg/dL";
                values["HDL"] = $"{random.Next(40, 80)} mg/dL";
                values["LDL"] = $"{random.Next(80, 160)} mg/dL";
                observations = cholesterol > 200 ? "Colesterol elevado - orientar dieta" : "Valores adequados";
                break;

            default:
                values["Resultado"] = "Normal";
                observations = "Exame sem altera��es";
                break;
        }

        return new ExamResult(
            Values: values,
            Observations: observations,
            ResultDate: DateTime.UtcNow.AddDays(-random.Next(0, 3))
        );
    }
}