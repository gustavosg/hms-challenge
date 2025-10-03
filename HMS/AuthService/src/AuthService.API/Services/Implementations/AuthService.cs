using AuthService.API.Data;
using AuthService.API.Models;
using AuthService.API.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

using Shared.DTOs.Auth;
using Shared.Infra.UnitOfWork;
using Shared.Mapper;
using Shared.Utils;

namespace AuthService.API.Services.Implementations;

public class AuthService(
    IAddMapper<Register, Models.User> registerMapper,
    IUnitOfWork<ApplicationDbContext> unitOfWork, 
    IConfiguration configuration
    ) : IAuthService
{
    public async Task<string> Login(Login request)
    {
        User user = await unitOfWork.Context.Users.SingleAsync(_ => _.Email == request.Email && !_.IsDeleted);

        bool passwordIsValid = PasswordHelper.Verify(request.Password, user.PasswordHash);
        if (!passwordIsValid)
            return string.Empty;

        // Usar as mesmas chaves que a API consome
        string secret = configuration["Jwt:Secret"]!;
        string issuer = configuration["Jwt:Issuer"]!;
        string audience = configuration["Jwt:Audience"]!;
        int expiration = int.TryParse(configuration["Jwt:ExpirationHours"], out var hours) ? hours : 24;

        return TokenHelper.GenerateToken(user.Id, user.Email, secret, issuer, audience, expiration);
    }

    public async Task<Guid> Register(Register request)
    {
        string passwordHash = PasswordHelper.Hash(request.Password);

        User user = registerMapper.ToEntity(request);
        user.PasswordHash = passwordHash;

        await unitOfWork.Context.Users.AddAsync(user);
        await unitOfWork.CommitAsync();

        return user.Id;
    }
}
