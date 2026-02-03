namespace Boltzenberg.Functions.Storage
{
    public enum ResultCode
    {
        Success,
        PreconditionFailed,
        GenericError
    };

    public class OperationResult<T>
    {
        public ResultCode Code { get; }
        public T? Entity { get; }
        public Exception? Error { get; }

        public OperationResult(ResultCode code, T? entity, Exception? error)
        {
            this.Code = code;
            this.Entity = entity;
            this.Error = error;
        }
    }
}