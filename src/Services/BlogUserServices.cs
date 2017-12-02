using System;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Miniblog.Core.Services
{
    public class BlogUserServices : IUserServices
    {
        private readonly IOptionsSnapshot<BlogSettings> _settings;

        public BlogUserServices(IOptionsSnapshot<BlogSettings> settings)
        {
            _settings = settings;
        }

        public bool ValidateUser(string username, string password)
        {

            return username == _settings.Value.Username && VerifyHashedPassword(password);
        }

        private bool VerifyHashedPassword(string password)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(_settings.Value.Salt);

            byte[] hashBytes = KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 1000,
                numBytesRequested: 256 / 8
            );

            string hashText = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            return hashText == _settings.Value.Password;
        }
    }
}
