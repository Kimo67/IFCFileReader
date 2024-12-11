using OpenTK.Mathematics;

namespace IFCReader.Models
{
    public abstract class IFCFigure
    {
        // Méthode abstraite pour le rendu OpenGL qui sera implémenté dans les classes qui en héritent
        public abstract void Render();

        // Méthode pour décoder un Point3D
       protected Vector3 DecodeCartesianPoint(string cartesianPoint)
        {
            var coords = cartesianPoint
                .Split(new[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(',')
                .Select(coord => coord.Trim())
                .Select(coord => float.Parse(coord, System.Globalization.CultureInfo.InvariantCulture)) // test
                .ToArray();

            return new Vector3(coords[0], coords[1], coords[2]);
        }


        // Méthode utilitaire pour décoder un Vector3
        protected Vector3 DecodeDirection(string direction)
        {
            var coords = direction
                .Split(new[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(',')
                .Select(coord => coord.Trim())
                .Select(coord => float.Parse(coord, System.Globalization.CultureInfo.InvariantCulture)) 
                .ToArray();

            return new Vector3(coords[0], coords[1], coords[2]);
        }

    }
}
