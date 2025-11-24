using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainMenuScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<LobbyService>(Lifetime.Singleton);
        
        // Register RelayService as a singleton
        builder.Register<RelayService>(Lifetime.Singleton);
        
        // Register ServicesInitializer as a singleton
        builder.Register<ServicesInitializer>(Lifetime.Singleton);

        builder.RegisterEntryPoint<ServicesInitializer>();
    }
}
