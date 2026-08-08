using System.Collections.Generic;

namespace Neurohard.Prompter
{
    /// <summary>
    /// Responde consultas que el diálogo no puede evaluar por sí mismo:
    /// inventario, hora del día, estado del mundo.
    /// </summary>
    public interface IQueryResolver
    {
        bool CanResolve(string queryName);

        /// <summary>
        /// Devuelve el valor de la consulta. Puede ser bool, número o cadena;
        /// se compara igual que una variable.
        /// </summary>
        object Resolve(string queryName, IReadOnlyList<string> arguments);
    }

    
}