using Microsoft.Extensions.Options;
using System;
using System.Collections;

namespace Miniblog.Core
{
    public class EnvironmentVariablesBlogSettings : IOptionsSnapshot<BlogSettings>
    {
        private readonly BlogSettings value;

        public EnvironmentVariablesBlogSettings(BlogSettings value)
        {
            this.value = value;
        }

        public BlogSettings Value => this.value;

        public BlogSettings Get(string name)
        {
            return this.value;
        }

        /// <summary>
        /// Tries to get values from environment variables
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        internal static bool TryGet(IDictionary variables, out IOptionsSnapshot<BlogSettings> settings)
        {
            settings = null;

            if (variables == null || variables.Count == 0)
                return false;

            var postsPerPageRaw = variables["mb-postsperpage"];
            if (postsPerPageRaw == null ||
                !int.TryParse(postsPerPageRaw.ToString(), out var postsPerPage) ||
                postsPerPage <= 0)
                return false;


            var commentsCloseAfterDaysRaw = variables["mb-commentsclosefterdays"];
            if (commentsCloseAfterDaysRaw == null ||
                !int.TryParse(commentsCloseAfterDaysRaw.ToString(), out var commentsCloseAfterDays) ||
                commentsCloseAfterDays <= 0)
                return false;

            var owner = variables["mb-owner"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(owner))
                return false;


            var username = variables["mb-username"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(username))
                return false;


            var pwdSalt = variables["mb-pwdsalt"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(pwdSalt))
                return false;

            var pwdHash = variables["mb-pwdhash"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(pwdHash))
                return false;

            var name = variables["mb-name"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
                return false;

            var shortName = variables["mb-shortname"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(shortName))
                return false;

            var description = variables["mb-description"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(description))
                return false;


            settings = new EnvironmentVariablesBlogSettings(
                new BlogSettings()
                {
                    Name = name,
                    ShortName = shortName,
                    Description = description,
                    Owner = owner,
                    PostsPerPage = postsPerPage,
                    CommentsCloseAfterDays = commentsCloseAfterDays,
                    Password = pwdHash,
                    Salt = pwdSalt,
                    Username = username,                    
                }
                );

            return true;
        }

        
    }
}
