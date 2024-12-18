// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Channels.Obsolete;

using FluentAssertions;
using Opc.Ua;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class NullChannelTests
{
    [Fact]
    public void SupportedFeaturesShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => { var _ = sut.SupportedFeatures; };

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*SupportedFeatures deprecated*");
    }

    [Fact]
    public void EndpointDescriptionShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => { var _ = sut.EndpointDescription; };

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*EndpointDescription deprecated*");
    }

    [Fact]
    public void EndpointConfigurationShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => { var _ = sut.EndpointConfiguration; };

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*EndpointConfiguration deprecated*");
    }

    [Fact]
    public void MessageContextShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => { var _ = sut.MessageContext; };

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*MessageContext deprecated*");
    }

    [Fact]
    public void CurrentTokenShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => { var _ = sut.CurrentToken; };

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*CurrentToken deprecated*");
    }

    [Fact]
    public void OnTokenActivatedAddShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.OnTokenActivated += (a, b, e) => { };

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*OnTokenActivated deprecated*");
    }

    [Fact]
    public void OnTokenActivatedRemoveShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.OnTokenActivated -= (a, b, e) => { };

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*OnTokenActivated deprecated*");
    }

    [Fact]
    public void BeginCloseShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.BeginClose(null!, null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*BeginClose deprecated*");
    }

    [Fact]
    public void BeginOpenShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.BeginOpen(null!, null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*BeginOpen deprecated*");
    }

    [Fact]
    public void BeginReconnectShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.BeginReconnect(null!, null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*BeginReconnect deprecated*");
    }

    [Fact]
    public void BeginSendRequestShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.BeginSendRequest(null!, null!, null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*BeginSendRequest deprecated*");
    }

    [Fact]
    public void CloseShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = sut.Close;

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*Close deprecated*");
    }

    [Fact]
    public async Task CloseAsyncShouldThrowNotSupportedAsync()
    {
        var sut = new NullChannel();

        // Act
        Func<Task> act = async () => await sut.CloseAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ServiceResultException>()
            .WithMessage("*CloseAsync deprecated*");
    }

    [Fact]
    public void EndCloseShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.EndClose(null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*EndClose deprecated*");
    }

    [Fact]
    public void EndOpenShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.EndOpen(null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*EndOpen deprecated*");
    }

    [Fact]
    public void EndReconnectShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.EndReconnect(null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*EndReconnect deprecated*");
    }

    [Fact]
    public void EndSendRequestShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.EndSendRequest(null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*EndSendRequest deprecated*");
    }

    [Fact]
    public async Task EndSendRequestAsyncShouldThrowNotSupportedAsync()
    {
        var sut = new NullChannel();

        // Act
        Func<Task> act = async () => await sut.EndSendRequestAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ServiceResultException>()
            .WithMessage("*EndSendRequestAsync deprecated*");
    }

    [Fact]
    public void InitializeWithUriShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.Initialize(new Uri("http://localhost"), null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*Initialize deprecated*");
    }

    [Fact]
    public void InitializeWithConnectionShouldThrowNotSupported()
    {
        var sut = new NullChannel();
        // Act
        Action act = () => sut.Initialize((ITransportWaitingConnection?)null!, null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*Initialize deprecated*");
    }

    [Fact]
    public void OpenShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = sut.Open;

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*Open deprecated*");
    }

    [Fact]
    public void ReconnectShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = sut.Reconnect;

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*Reconnect deprecated*");
    }

    [Fact]
    public void ReconnectWithConnectionShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.Reconnect(null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*Reconnect deprecated*");
    }

    [Fact]
    public void SendRequestShouldThrowNotSupported()
    {
        var sut = new NullChannel();

        // Act
        Action act = () => sut.SendRequest(null!);

        // Assert
        act.Should().Throw<ServiceResultException>()
            .WithMessage("*SendRequest deprecated*");
    }

    [Fact]
    public async Task SendRequestAsyncShouldThrowNotSupportedAsync()
    {
        var sut = new NullChannel();

        // Act
        Func<Task> act = async () => await sut.SendRequestAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ServiceResultException>()
            .WithMessage("*SendRequestAsync deprecated*");
    }
}
