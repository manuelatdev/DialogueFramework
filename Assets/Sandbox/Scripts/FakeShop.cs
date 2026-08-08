using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Neurohard.Prompter;

/// <summary>Tienda de mentira: responde consultas y ejecuta comandos del diálogo.</summary>
public sealed class FakeShop : MonoBehaviour, IQueryResolver, ICommandDispatcher
{
    [SerializeField] private int oro = 40;

    private readonly Dictionary<string, int> _precios = new Dictionary<string, int> {
        ["cuerda"] = 10, ["pocion"] = 25, ["antorcha"] = 5, ["amuleto"] = 100
    };

    private readonly Dictionary<string, int> _stock = new Dictionary<string, int> {
        ["cuerda"] = 3, ["pocion"] = 2, ["antorcha"] = 5, ["amuleto"] = 1
    };

    private readonly Dictionary<string, int> _inventario = new Dictionary<string, int> {
        ["piel"] = 2
    };

    private float _descuento = 1f;

    public int Oro => oro;

    // --- consultas --------------------------------------------------------

    public bool CanResolve(string queryName)
        => queryName == "puede_comprar" || queryName == "tiene_objeto";

    public object Resolve(string queryName, IReadOnlyList<string> args)
    {
        if (args.Count == 0) return false;
        var item = args[0];

        switch (queryName)
        {
            case "puede_comprar":
                return _precios.TryGetValue(item, out var precio)
                    && _stock.GetValueOrDefault(item) > 0
                    && oro >= Mathf.RoundToInt(precio * _descuento);

            case "tiene_objeto":
                return _inventario.GetValueOrDefault(item) > 0;

            default:
                return false;
        }
    }

    // --- comandos ---------------------------------------------------------

    public bool CanHandle(string commandName)
        => commandName == "comprar" || commandName == "vender" || commandName == "aplicar_descuento";

    public Task DispatchAsync(DialogueStep.Command command, CancellationToken ct)
    {
        var item = command.Arguments.Count > 0 ? command.Arguments[0] : null;

        switch (command.Name)
        {
            case "comprar":
                var coste = Mathf.RoundToInt(_precios[item] * _descuento);
                oro -= coste;
                _stock[item]--;
                _inventario[item] = _inventario.GetValueOrDefault(item) + 1;
                Debug.Log($"[Tienda] Compras {item} por {coste}. Te quedan {oro} monedas.");
                break;

            case "vender":
                _inventario[item]--;
                oro += 12;
                Debug.Log($"[Tienda] Vendes {item}. Ahora tienes {oro} monedas.");
                break;

            case "aplicar_descuento":
                _descuento = 0.8f;
                Debug.Log("[Tienda] Descuento del 20% aplicado.");
                break;
        }

        return Task.CompletedTask;
    }
}