using System;
using System.Collections.Generic;
using IFCReader.Models;   // IFCFigure

namespace IFCReader.Scene
{
    /// Scène complète : figures + texte de résumé de ce qui a été affiché
    public class Scene
    {
        public List<IFCFigure> Figures { get; }
        private readonly string _summary;

        public Scene(List<IFCFigure> figures, string summary)
        {
            Figures = figures;
            _summary = summary;
        }

        public void LogSummary() => Console.WriteLine(_summary);
    }
}