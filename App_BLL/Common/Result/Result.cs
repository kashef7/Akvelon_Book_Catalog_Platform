namespace App_BLL.Common.Result;


public class Result
{
    public bool IsSuccess { get; protected set; }
    public ErrorType? Error { get; protected set; }
    public string? Message { get; protected set; }

    protected Result(bool isSuccess, ErrorType? error, string? message)
    {
        IsSuccess = isSuccess;
        Error = error;
        Message = message;
    }

    public static Result Success(string message = "Success")
    {
        return new Result(true, null, message);
    }

    public static Result Failed(ErrorType error, string message = "Failed")
    {
        return new Result(false, error, message);
    }
}

public class Result<T> : Result
{
    public T? Data { get; private set; }

    private Result(T? data, bool isSuccess, ErrorType? error, string? message) 
        : base(isSuccess, error, message)
    {
        Data = data;
    }

    public static Result<T> Success(T data, string message = "Success")
    {
        return new Result<T>(data, true, null, message);
    }

    public new static Result<T> Failed(ErrorType error, string message = "Failed")
    {
        return new Result<T>(default, false, error, message);
    }
}