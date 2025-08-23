namespace Polymerium.Trident.Exceptions
{
    public class MinecraftGameNotOwnedException(string message = "The account does not own the game")
        : AccountAuthenticationException(message);
}
