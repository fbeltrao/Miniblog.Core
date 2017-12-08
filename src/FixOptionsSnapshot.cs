using System;
using Microsoft.Extensions.Options;

namespace Miniblog.Core
{
    /// <summary>
    /// Fix <see cref="IOptionsSnapshot"/> implementation
    /// </summary>
    public class FixOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions> where TOptions : class, new()
    {
        private readonly TOptions options;

        public FixOptionsSnapshot(TOptions options)
        {
            this.options = options;
        }

        public TOptions Value => this.options;

        public TOptions Get(string name)
        {
            return this.options;
        }
    }
}
