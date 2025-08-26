using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nizamla.Infrastructure.Auth
{
    public interface IRefreshTokenPolicy
    {
        TimeSpan LifeSpan { get; }
    }
    public sealed class DefaultRefreshTokenPolicy : IRefreshTokenPolicy
    {
        public DefaultRefreshTokenPolicy(TimeSpan lifeSpan) =>  LifeSpan = lifeSpan;
        public TimeSpan LifeSpan { get; }

    }
}
