using Kurisu.AspNetCore.Utils;

namespace Kurisu.Test.WebApi_A
{
    public class GroupResponse 
    {
        public Guid? PCode { get; set; }

        public Guid Code { get; set; }

        public List<GroupResponse> Next { get; set; }
    }
}
