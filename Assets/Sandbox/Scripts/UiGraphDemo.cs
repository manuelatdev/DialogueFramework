using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Neurohard.Prompter;
using Neurohard.Prompter.Unity;
using Neurohard.Playwright.Unity;

public sealed class UiGraphDemo : MonoBehaviour
{
    [SerializeField] private DialogueGraphAsset graphAsset;
    [SerializeField] private UiToolkitPresenter presenter;
    [SerializeField] private int carismaInicial = 1;

    private readonly InMemoryVariableStorage _vars = new InMemoryVariableStorage();
    private PrompterBehaviour _prompter;
    private FakeShop _tienda;

    private void Start()
    {
        if (graphAsset == null || presenter == null)
        {
            Debug.LogError("Faltan referencias en el inspector.");
            return;
        }

        _tienda = gameObject.AddComponent<FakeShop>();

        _vars.Set("carisma", carismaInicial);
        _vars.Set("descuento_concedido", false);
        _vars.Set("compras_realizadas", 0);

        _prompter = gameObject.AddComponent<PrompterBehaviour>();
        _prompter.AddPresenter(presenter);
        _prompter.Input = presenter.Input;
        _prompter.Variables = _vars;
        _prompter.Commands = _tienda;
        _prompter.Queries = _tienda;

        Hablar();
    }

    private async void Hablar()
    {
        var result = await _prompter.Play(graphAsset.CreateSource());
        Debug.Log($"Terminada: {result.Outcome}");
        DumpVars();
    }

    private void Update()
    {
        if (_prompter == null || _prompter.IsPlaying) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            Hablar();
    }

    private void DumpVars()
    {
        foreach (var name in new[] { "carisma", "compras_realizadas", "descuento_concedido" })
            Debug.Log(_vars.TryGet<object>(name, out var v) ? $"{name} = {v}" : $"{name} = (sin definir)");

        Debug.Log($"oro (tienda) = {_tienda.Oro}");
    }
}