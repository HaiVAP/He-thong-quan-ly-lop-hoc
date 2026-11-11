namespace EduHub.Api.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public List<string>? Errors { get; }

    public AppException(string message, int statusCode = 400, List<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message = "Không tìm thấy dữ liệu yêu cầu.")
        : base(message, 404) { }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message, List<string>? errors = null)
        : base(message, 400, errors) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.")
        : base(message, 401) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Bạn không có quyền thực hiện hành động này.")
        : base(message, 403) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message = "Dữ liệu bị xung đột hoặc đã tồn tại.")
        : base(message, 409) { }
}

