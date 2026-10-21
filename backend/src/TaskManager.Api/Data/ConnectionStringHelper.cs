using Npgsql;

namespace TaskManager.Api.Data;

/// <summary>
/// Resolve a connection string. Aceita DATABASE_URL (padrão de Vercel/Neon/Supabase/Render, formato URI)
/// ou ConnectionStrings:Default (formato chave=valor do Npgsql).
/// </summary>
public static class ConnectionStringHelper
{
    public static string Resolve(IConfiguration config)
    {
        var raw = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(raw)) raw = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Connection string ausente. Defina ConnectionStrings__Default ou DATABASE_URL.");
        return Normalize(raw);
    }

    public static string Normalize(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Require
        }.ConnectionString;
    }
}
