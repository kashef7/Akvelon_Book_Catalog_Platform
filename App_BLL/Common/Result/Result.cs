namespace App_BLL.Common.Result;

public class Result <T>
{
    public T? Data { get; private set; }
    public bool IsSuccess { get; private set; }
    public int StatusCode { get; private set; }

    public string? Message { get; private set; }

    private Result (T? data, bool isSuccess, int statusCode, string? message)
    {
        Data = data;
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Message = message;
        
    }

    public static Result<T> Success(T data , int statusCode = 200, string message = "Success")
    {
        return new (data, true, statusCode, message);
    }

    public static Result<T> Failed(int statusCode, string message = "Failed")
    {
        return new(default, false, statusCode, message);
    }

}