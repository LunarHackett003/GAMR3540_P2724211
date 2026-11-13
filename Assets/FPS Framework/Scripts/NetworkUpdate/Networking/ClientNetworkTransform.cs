using Unity.Netcode.Components;


public enum AuthorityMode : int
{
    Server = 0,
    Client = 1,
}

public class ClientNetworkTransform : NetworkTransform
{
    public AuthorityMode authorityMode = global::AuthorityMode.Client;
    protected override bool OnIsServerAuthoritative() => authorityMode == global::AuthorityMode.Server;
}
