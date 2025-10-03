using AuthService.API.Services.Interfaces;

using Microsoft.AspNetCore.Mvc;

using Shared.DTOs;

using AuthDTO = Shared.DTOs.Auth;
using UserDTO = Shared.DTOs.Users;

namespace AuthService.API.Extensions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapUserEndpoints();

        return app;
    }

    private static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(PrefixEndpoints.Auth).WithTags(TagsEndpoints.Auth);

        group.MapPost("/login", async (
            [FromBody] AuthDTO.Login login,
            [FromServices] IAuthService authService,
            [FromServices] IUserService userService) =>
        {
            if (!await userService.ExistsAsync(_ => _.Email == login.Email && !_.IsDeleted))
                return Results.NotFound("User does not exist");

            string token = await authService.Login(login);

            if (string.IsNullOrWhiteSpace(token))
                return Results.Unauthorized();

            return Results.Ok(new { Token = token });
        });

        group.MapPost("/register", async (
            [FromBody] AuthDTO.Register register,
            [FromServices] IUserService userService,
            [FromServices] IAuthService authService) =>
        {
            if (await userService.ExistsAsync(_ => _.Username == register.Username && !_.IsDeleted))
                return Results.Conflict("User already exists");

            Guid userId = await authService.Register(register);

            return Results.Ok(userId);
        });
        return group;
    }

    private static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(PrefixEndpoints.User).WithTags(TagsEndpoints.User);

        group.MapPost("/", async (
            [FromBody] UserDTO.Add.Request request,
            [FromServices] IUserService service) =>
        {
            if (await service.ExistsAsync(_ => _.Username == request.Username && !_.IsDeleted))
                return Results.Conflict("User already exists");

            Guid userId = await service.AddAsync(request);

            return Results.Ok(userId);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UserDTO.Edit.Request request,
            [FromServices] IUserService service) =>
        {
            if (!await service.ExistsAsync(_ => _.Id == id && !_.IsDeleted))
                return Results.NotFound("User does not exist");

            request = request with { Id = id };
            UserDTO.Get.Response response = await service.EditAsync(request);

            return Results.Ok(response);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService service) =>
        {
            if (!await service.ExistsAsync(_ => _.Id == id && !_.IsDeleted))
                return Results.NotFound("User does not exist");

            var user = await service.GetAsync(id);

            return Results.Ok(user);
        });

        group.MapGet("/", async (
            [FromServices] IUserService service,
            [AsParameters] UserDTO.Get.Request request,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            PaginationResponse<UserDTO.Get.Response> data = await service.GetAsync(request, page, pageSize);
            return Results.Ok(data);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IUserService service) =>
        {
            if (!await service.ExistsAsync(_ => _.Id == id && !_.IsDeleted))
                return Results.NotFound("User does not exist");

            bool deleted = await service.DeleteAsync(id);

            if (deleted)
                return Results.NoContent();

            return Results.Problem("Failed to delete user", statusCode: 500);
        });

        return group.RequireAuthorization();
    }

    private static class PrefixEndpoints
    {
        internal const string Auth = "/api/auth";
        internal const string User = "/api/users";
    }

    private static class TagsEndpoints
    {
        internal const string Auth = "Authentication";
        internal const string User = "Users";
    }

}
