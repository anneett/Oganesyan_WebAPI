using Microsoft.AspNetCore.DataProtection;
using Oganesyan_WebAPI.Models;

namespace Oganesyan_WebAPI.Services
{
    public class ConnectionStringProtectionService
    {
        private readonly IDataProtector _protector;

        public ConnectionStringProtectionService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("Oganesyan_WebAPI.ConnectionStrings.v1");
        }

        public string Protect(string plainText)
        {
            return _protector.Protect(plainText);
        }

        public string Unprotect(string protectedText)
        {
            return _protector.Unprotect(protectedText);
        }

        public string Mask(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return string.Empty;
            }

            var parts = connectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Select(part =>
                {
                    var separatorIndex = part.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        return part;
                    }

                    var key = part[..separatorIndex].Trim();
                    var value = part[(separatorIndex + 1)..].Trim();

                    if (key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                        key.Contains("pwd", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{key}=********";
                    }

                    if (value.Length <= 4)
                    {
                        return $"{key}={value}";
                    }

                    return $"{key}={value[..2]}***{value[^2..]}";
                });

            return string.Join("; ", parts);
        }

        public string MaskProtected(DbMeta dbMeta)
        {
            return Mask(Unprotect(dbMeta.ConnectionString));
        }
    }
}
