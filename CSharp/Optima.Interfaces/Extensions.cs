using System;
using Google.Protobuf.WellKnownTypes;

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

namespace Optima.Domain.Core
{
    public partial class Result
    {
        // ReSharper disable InconsistentNaming
        public static Result SUCCESS = new Result { Success = new Empty() };
        public static Result FAILURE(string reason) => new Result { Failure = reason };
        // ReSharper restore InconsistentNaming
    }
}