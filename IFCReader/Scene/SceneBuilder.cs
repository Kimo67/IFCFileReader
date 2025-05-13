using System.Collections.Generic;
using System.Text;
using IFCReader.Models;   // IFCFigure

namespace IFCReader.Scene
{
    public class SceneBuilder
    {
        private readonly List<IProductExtractor> _extractors = new();

        public SceneBuilder AddExtractor(IProductExtractor extractor)
        {
            _extractors.Add(extractor);
            return this;
        }

        public Scene Build(Dictionary<string, Dictionary<string,string>> db)
        {
            var figures = new List<IFCFigure>();
            var sb      = new StringBuilder();

            foreach (var ex in _extractors)
            {
                ex.Extract(db, figures);
                sb.AppendLine(ex.SummaryLine);
            }
            return new Scene(figures, sb.ToString());
        }
    }
}