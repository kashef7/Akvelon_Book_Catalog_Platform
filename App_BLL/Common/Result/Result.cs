namespace App_BLL.Common.Result;

// TODO: Clean Code - Result exposes raw HTTP status codes to the BLL; consider an error-kind enum instead

public class Result
{
    public bool IsSuccess { get; protected set; }
    public int StatusCode { get; protected set; }
    public string? Message { get; protected set; }

    protected Result(bool isSuccess, int statusCode, string? message)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Message = message;
    }

    public static Result Success(int statusCode = 200, string message = "Success")
    {
        return new Result(true, statusCode, message);
    }

    public static Result Failed(int statusCode, string message = "Failed")
    {
        return new Result(false, statusCode, message);
    }
}

public class Result<T> : Result
{
    public T? Data { get; private set; }

    private Result(T? data, bool isSuccess, int statusCode, string? message) 
        : base(isSuccess, statusCode, message)
    {
        Data = data;
    }

    public static Result<T> Success(T data, int statusCode = 200, string message = "Success")
    {
        return new Result<T>(data, true, statusCode, message);
    }

    public new static Result<T> Failed(int statusCode, string message = "Failed")
    {
        return new Result<T>(default, false, statusCode, message);
    }
}