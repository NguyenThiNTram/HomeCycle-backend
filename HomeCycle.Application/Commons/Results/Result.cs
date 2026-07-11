using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Results
{
    public class Result
    {
        public bool IsSuccess { get; }
        public Error Error { get; }

        protected Result(bool isSuccess, Error error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, Error.None);
        public static Result Fail(Error error) => new(false, error);
    }
    public class Result<T> : Result
    {
        public T? Data { get; }

        private Result(bool isSuccess, T? data, Error error) : base(isSuccess, error)
        {
            Data = data;
        }

        public static Result<T> Success(T data) => new(true, data, Error.None);
        public static Result<T> Fail(Error error) => new(false, default, error);
    }
}
