using System.ComponentModel.DataAnnotations.Schema;
using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Entity;

namespace Kurisu.Test.WebApi_A.Entity;

[EnableSplitTable]
[Table("test")]
public class Test : BaseEntity<int, int?>
{
    public string Name { get; set; }
}