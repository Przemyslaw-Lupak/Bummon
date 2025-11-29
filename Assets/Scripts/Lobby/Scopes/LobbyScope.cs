using UnityEngine;
using VContainer;
using VContainer.Unity;

public class LobbyScope : LifetimeScope
{
    [SerializeField] private LobbyView lobbyView;
    [SerializeField] private ConnectionView connectionView;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(lobbyView);
        builder.RegisterComponent(connectionView);
    }
}

