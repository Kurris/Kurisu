using Kurisu.DataAccessor.Dto;

namespace Kurisu.Gateway.Dto.Input
{
    public class GetProjectRoutesPageListInput : PageInput
    {
        public string Keyword { get; set; }

        public bool? Enable { get; set; }

        public int ProjectId { get; set; }
    }
}
