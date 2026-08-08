using System;
using System.Collections.Generic;

namespace Neurohard.Prompter
{
    /// <summary>
    /// Resolver por defecto: no resuelve nada y avisa. Las condiciones que
    /// dependan de una consulta se evaluarán como no cumplidas.
    /// </summary>
    public sealed class NoQueryResolver : IQueryResolver
    {
        private readonly Action<string> _log;

        public NoQueryResolver(Action<string> log = null) => _log = log ?? (_ => { });

        public bool CanResolve(string queryName) => false;

        public object Resolve(string queryName, IReadOnlyList<string> arguments)
        {
            _log($"[Prompter] Consulta '{queryName}' sin resolver: no hay IQueryResolver registrado. " +
                 "La condición se evaluará como no cumplida.");
            return null;
        }
    }
}