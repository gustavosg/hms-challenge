namespace PatientsService.API.Utils;

public static class DefaultPasswordHelper
{
    /// <summary>
    /// Gera uma senha padr�o baseada no documento (CPF) do paciente
    /// Formato: Primeiros 4 d�gitos do CPF + "@Patient" 
    /// Exemplo: CPF 12345678901 -> senha "1234@Patient"
    /// </summary>
    /// <param name="document">Documento (CPF) do paciente</param>
    /// <returns>Senha padr�o gerada</returns>
    public static string GenerateDefaultPassword(string document)
    {
        if (string.IsNullOrWhiteSpace(document) || document.Length < 4)
            return "Default@123";
        
        // Pega os primeiros 4 d�gitos do CPF e adiciona sufixo
        var firstFourDigits = document.Substring(0, 4);
        return $"{firstFourDigits}@Patient";
    }
}