using System;

namespace Optima.Domain.Security
{
    public sealed partial class Principal
    {
        public Core.UUID Id => KindCase switch {
            KindOneofCase.User => User.Id,
            KindOneofCase.Group => Group.Id,
            KindOneofCase.Role => Role.Id,
            KindOneofCase.None => throw new NotImplementedException(),
            _ => throw new NotImplementedException()
        };
    }
}