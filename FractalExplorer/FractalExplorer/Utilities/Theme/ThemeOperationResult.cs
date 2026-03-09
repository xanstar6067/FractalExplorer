namespace FractalExplorer.Utilities.Theme
{
    public sealed class ThemeOperationResult
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }

        private ThemeOperationResult(bool isSuccess, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static ThemeOperationResult Success() => new(true, null, null);
        public static ThemeOperationResult Failure(string message, Exception? exception = null) => new(false, message, exception);
    }

    public sealed class ThemeOperationResult<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }

        private ThemeOperationResult(bool isSuccess, T? value, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static ThemeOperationResult<T> Success(T value) => new(true, value, null, null);
        public static ThemeOperationResult<T> Failure(string message, Exception? exception = null) => new(false, default, message, exception);
    }
}
