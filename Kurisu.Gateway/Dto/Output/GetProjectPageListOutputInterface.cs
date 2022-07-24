using System;

namespace Kurisu.Gateway.Dto.Output
{
    public class GetProjectPageListOutput
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int OrderNo { get; set; }

        public bool Enable { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
