using Stateless;
using System;
using System.Threading.Tasks;

internal sealed class SessionStateMachine
{
    /// <summary>
    /// State of the session
    /// </summary>
    public State CurrentState => _machine.State;

    public SessionStateMachine()
    {
        _machine = new StateMachine<State, Trigger>(State.Closed);

        _createTrigger = _machine.SetTriggerParameters<TaskCompletionSource<bool>>(Trigger.Create);

        _machine.Configure(State.Closed)
            .Permit(Trigger.Create, State.Connecting)
            .Permit(Trigger.Close, State.Closed);

        _machine.Configure(State.Connecting)
            .OnEntryAsync(() => OnCreateAsync())
            .Permit(Trigger.CreateSuccessful, State.Connected)
            .Permit(Trigger.CreateFailed, State.Error)
            .Permit(Trigger.Close, State.Closed);

        _machine.Configure(State.Connected)
            .Permit(Trigger.Reset, State.Connecting)
            .Permit(Trigger.KeepAliveMissing, State.Reconnecting)
            .Permit(Trigger.RenewUserIdentity, async () => await OnRenewUserIdentityAsync());

        _machine.Configure(State.Disconnected)
            .Permit(Trigger.Reconnect, async () => await OnReconnectAsync())
            .Permit(Trigger.Close, State.Closed);

        _machine.Configure(State.Reconnecting)
            .Permit(Trigger.ReconnectSuccessful, State.Connected)
            .Permit(Trigger.ReconnectFailed, State.Error)
            .Permit(Trigger.Close, State.Closed);

        _machine.Configure(State.Reactivating)
            .Permit(Trigger.RenewalSuccessful, State.Connected)
            .Permit(Trigger.RenewalFailed, async () => await OnRenewalFailedAsync());

        _machine.Configure(State.Error)
            .Permit(Trigger.Create, State.Connecting)
            .Permit(Trigger.Close, State.Closed);
    }

    private async Task<State> OnCreateAsync()
    {
        await Task.Delay(1000); // Simulate async work
        return State.Connected;
    }

    private async Task<State> OnCreateSuccessfulAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Connected;
    }

    private async Task<State> OnCreateFailedAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Error;
    }

    private async Task<State> OnResetAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Connecting;
    }

    private async Task<State> OnKeepAliveMissingAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Disconnected;
    }

    private async Task<State> OnReconnectAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Reconnecting;
    }

    private async Task<State> OnReconnectSuccessfulAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Connected;
    }

    private async Task<State> OnReconnectFailedAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Connecting;
    }

    private async Task<State> OnRenewUserIdentityAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Reactivating;
    }

    private async Task<State> OnRenewalSuccessfulAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Connected;
    }

    private async Task<State> OnRenewalFailedAsync()
    {
        await Task.Delay(500); // Simulate async work
        return State.Reconnecting;
    }

    public async Task FireAsync(Trigger trigger)
    {
        await _machine.FireAsync(trigger);
    }

    public async Task FireCreateAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        await _machine.FireAsync(_createTrigger, tcs);
    }

    public async Task FireCreateSuccessfulAsync()
    {
        await _machine.FireAsync(Trigger.CreateSuccessful);
    }

    public async Task FireCreateFailedAsync()
    {
        await _machine.FireAsync(Trigger.CreateFailed);
    }

    public async Task FireCloseAsync()
    {
        await _machine.FireAsync(Trigger.Close);
    }

    public async Task FireResetAsync()
    {
        await _machine.FireAsync(Trigger.Reset);
    }

    public async Task FireKeepAliveMissingAsync()
    {
        await _machine.FireAsync(Trigger.KeepAliveMissing);
    }

    public async Task FireReconnectAsync()
    {
        await _machine.FireAsync(Trigger.Reconnect);
    }

    public async Task FireReconnectSuccessfulAsync()
    {
        await _machine.FireAsync(Trigger.ReconnectSuccessful);
    }

    public async Task FireReconnectFailedAsync()
    {
        await _machine.FireAsync(Trigger.ReconnectFailed);
    }

    public async Task FireRenewUserIdentityAsync()
    {
        await _machine.FireAsync(Trigger.RenewUserIdentity);
    }

    public async Task FireRenewalSuccessfulAsync()
    {
        await _machine.FireAsync(Trigger.RenewalSuccessful);
    }

    public async Task FireRenewalFailedAsync()
    {
        await _machine.FireAsync(Trigger.RenewalFailed);
    }
    internal enum State
    {
        Closed,
        Connecting,
        Connected,
        Disconnected,
        Reconnecting,
        Reactivating,
        Error
    }

    internal enum Trigger
    {
        Create,
        CreateSuccessful,
        CreateFailed,
        Close,
        Reset,
        KeepAliveMissing,
        Reconnect,
        ReconnectSuccessful,
        ReconnectFailed,
        RenewUserIdentity,
        RenewalSuccessful,
        RenewalFailed
    }

    private readonly StateMachine<State, Trigger> _machine;
    private readonly StateMachine<State, Trigger>.TriggerWithParameters<TaskCompletionSource<bool>> _createTrigger;
}
