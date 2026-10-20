namespace TaskManager.Api.Exceptions;

public class NotFoundException(string message) : Exception(message);

public class InvalidCredentialsException() : Exception("E-mail ou senha inválidos.");
