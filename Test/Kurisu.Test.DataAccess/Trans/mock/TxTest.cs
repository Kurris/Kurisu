using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;

namespace Kurisu.Test.DataAccess.Trans.Mock;

public class TxTest : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}