using System.Collections.Generic;
using IFCReader.Models;   // IFCFigure

namespace IFCReader.Scene
{
    ///Contrat pour chaque type d'objet IFC à extraire
    public interface IProductExtractor
    {
        void Extract(Dictionary<string, Dictionary<string,string>> db,
                     List<IFCFigure> figs);
        string SummaryLine { get; }
    }
}