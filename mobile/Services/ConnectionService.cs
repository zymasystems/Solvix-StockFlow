using Microsoft.Maui.Devices;

namespace Solvix.Sims.Services;

/// <summary>
/// Network connectivity for the mobile app.
/// Inject this into pages/components to show online/offline state and to gate web API calls while offline.
/// </summary>
public sealed class ConnectionService
{
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public IReadOnlyList<ConnectionProfile> ConnectionProfiles =>
        Connectivity.Current.ConnectionProfiles.ToList();

    public event EventHandler<ConnectivityChangedEventArgs>? Changed;

    public ConnectionService()
    {
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    /// <summary>Waits until the device has internet access, or until <paramref name="timeout"/> elapses.</summary>
    public async Task<bool> WaitForConnectionAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (IsConnected) return true;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout is { } t) cts.CancelAfter(t);

        var tcs = new TaskCompletionSource<bool>();
        EventHandler<ConnectivityChangedEventArgs> handler = (_, args) =>
        {
            if (args.NetworkAccess == NetworkAccess.Internet && !cts.Token.IsCancellationRequested)
                tcs.TrySetResult(true);
        };
        Changed += handler;
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        finally
        {
            Changed -= handler;
            cts.Dispose();
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e) =>
        Changed?.Invoke(sender, e);
}
