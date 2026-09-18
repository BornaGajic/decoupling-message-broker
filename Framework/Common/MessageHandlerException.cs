namespace Framework.Common;

public class MessageHandlerException : Exception
{
    public MessageHandlerException(string message)
        : base(message)
    {
    }
}