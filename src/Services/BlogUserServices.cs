using System;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Miniblog.Core.Services
{

    public class BlogUserServices : IUserServices
    {
        private readonly IConfiguration _config;

        public BlogUserServices(IConfiguration config)
        {
            _config = config;
        }

        public bool ValidateUser(string username, string password)
        {
            var expectedUsername = _config["user_username"] ?? _config["user:username"]; 
            return username == expectedUsername && VerifyHashedPassword(password, _config);
        }

        private bool VerifyHashedPassword(string password, IConfiguration config)
        {
            var saltRaw = config["user_salt"] ?? config["user:salt"];
            byte[] saltBytes = Encoding.UTF8.GetBytes(saltRaw);

            byte[] hashBytes = KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 1000,
                numBytesRequested: 256 / 8
            );

            string hashText = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            var expectedHashText = config["user_password"] ?? config["user:password"];
            return hashText == expectedHashText;
        }
    }
}
