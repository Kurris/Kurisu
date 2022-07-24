using Kurisu.DataAccessor.Dto;

namespace Kurisu.Gateway.Dto.Input
{
    public class GetProjectPageListInput : PageInput
    {
        public string Name { get; set; }

        public bool? Enable { get; set; }
    }
}
