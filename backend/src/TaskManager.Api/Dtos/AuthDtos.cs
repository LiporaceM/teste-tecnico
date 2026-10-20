namespace TaskManager.Api.Dtos;

public record RegisterRequest(string FullName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record UserInfo(string Id, string Email, string FullName, string Role);
public record AuthResponse(string Token, DateTime ExpiresAt, UserInfo User);
