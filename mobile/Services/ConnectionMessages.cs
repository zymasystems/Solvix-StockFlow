using Microsoft.AspNetCore.Components;

namespace Solvix.Sims.Services;
public sealed class ConnectionMessages
{
    public string? Error { get; private set; }

    public event Action? Changed;
    public void Show(Exception ex)
    {
        Error = ex is ApiException api ? api.Detail ?? api.Message : ex.Message;
        Changed?.Invoke();
    }

    public void Clear()
    {
        Error = null;
        Changed?.Invoke();
    }
}

/// <summary>Reports failed asynchronous actions without replacing the supplied screen or continuing its success path.</summary>
public class ConnectedComponent : ComponentBase, IHandleEvent
{
    [Inject]
    protected ConnectionMessages Messages { get; set; } = default!;

    async Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        try
        {
            var task = callback.InvokeAsync(arg);
            StateHasChanged();
            await task;
        }
        catch (Exception ex)
        {
            Messages.Show(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }
}
