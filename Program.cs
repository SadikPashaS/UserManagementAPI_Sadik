
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var tokenFromConfig = builder.Configuration["Auth:Token"] ?? "techhive-2026";

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TokenAuthMiddleware>(tokenFromConfig);
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

var users = new ConcurrentDictionary<int, User>(new[]
{
    new KeyValuePair<int, User>(1, new User { UserId = 1, Username = "Sadik", Email = "sadik@example.com" }),
    new KeyValuePair<int, User>(2, new User { UserId = 2, Username = "Raman", Email = "raman@example.com" })
});

app.MapGet("/users", (int? skip, int? take) =>
{
    int s = Math.Max(0, skip ?? 0);
    int t = Math.Clamp(take ?? 100, 1, 500);

    var all = users.Values.OrderBy(u => u.UserId);
    var total = all.Count();
    var page = all.Skip(s).Take(t).ToList();

    return Results.Ok(new PagedResult<User>(page, total, s, t));
});

app.MapGet("/users/{id:int}", (int id) =>
{
    return users.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound(new { message = $"User with id {id} was not found." });
});

app.MapPost("/users", (User user) =>
{
    if (user is null)
        return Results.BadRequest(new { message = "Request body is required." });

    var (isValid, errors) = ValidationHelpers.Validate(user);
    if (!isValid)
        return Results.ValidationProblem(errors);

    if (users.ContainsKey(user.UserId))
        return Results.Conflict(new { message = $"UserId {user.UserId} already exists." });

    if (users.Values.Any(u => string.Equals(u.Username, user.Username, StringComparison.OrdinalIgnoreCase)))
        return Results.Conflict(new { message = $"Username '{user.Username}' is already taken." });

    var added = users.TryAdd(user.UserId, user);
    if (!added)
        return Results.Conflict(new { message = "Could not add the user due to a concurrent update. Try again." });

    return Results.Created($"/users/{user.UserId}", user);
});

app.MapPut("/users/{id:int}", (int id, User updated) =>
{
    if (updated is null)
        return Results.BadRequest(new { message = "Request body is required." });

    if (updated.UserId != id)
        return Results.BadRequest(new { message = "Route id and body userId must match." });

    var (isValid, errors) = ValidationHelpers.Validate(updated);
    if (!isValid)
        return Results.ValidationProblem(errors);

    if (!users.ContainsKey(id))
        return Results.NotFound(new { message = $"User with id {id} was not found." });

    if (users.Values.Any(u => u.UserId != id &&
                              string.Equals(u.Username, updated.Username, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.Conflict(new { message = $"Username '{updated.Username}' is already taken." });
    }

    users[id] = updated;
    return Results.NoContent();
});

app.MapDelete("/users/{id:int}", (int id) =>
{
    if (!users.TryRemove(id, out _))
        return Results.NotFound(new { message = $"User with id {id} was not found." });

    return Results.NoContent();
});

app.MapGet("/users/throw", () =>
{
    throw new InvalidOperationException("Test exception to verify error middleware.");
});

app.Run();
