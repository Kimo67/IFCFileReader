using System.Collections.Generic;

namespace IFCFileReader.Models
{
    public class IFCDataModel
    {
        public Dictionary<string, List<IFCEntity>> Entities { get; set; } = new();
    }
}
