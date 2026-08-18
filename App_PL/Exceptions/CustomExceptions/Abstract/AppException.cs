namespace App_PL.Exceptions.CustomExceptions.Abstract;

public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    protected AppException(string message) : base(message) { }
}