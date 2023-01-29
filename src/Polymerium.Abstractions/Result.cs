using System;

namespace Polymerium.Abstractions
{
    public class Result<TOk, TErr>
    {
        public static Result<TOk, TErr> Ok(TOk value)
        {
            return new Ok<TOk, TErr>(value);
        }

        public static Result<TOk, TErr> Err(TErr value)
        {
            return new Err<TOk, TErr>(value);
        }

        public bool IsOk(out TOk value)
        {
            if (this is Ok<TOk, TErr> ok)
            {
                value = ok.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool IsErr(out TErr value)
        {
            if (this is Err<TOk, TErr> err)
            {
                value = err.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public TOk Unwrap()
        {
            if (IsOk(out TOk value))
            {
                return value;
            }
            else
            {
                throw new NullReferenceException(typeof(TOk).FullName);
            }
        }
    }

    public class Result<TErr>
    {
        public static Ok<TErr> Ok()
        {
            return new Ok<TErr>();
        }

        public static Err<TErr> Err(TErr value)
        {
            return new Err<TErr>(value);
        }

        public bool IsOk()
        {
            return this is Ok<TErr>;
        }

        public bool IsErr(out TErr value)
        {
            if (this is Err<TErr> err)
            {
                value = err.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public TErr Unwrap()
        {
            if (IsErr(out TErr value))
            {
                return value;
            }
            else
            {
                throw new NullReferenceException(typeof(TErr).FullName);
            }
        }
    }
}