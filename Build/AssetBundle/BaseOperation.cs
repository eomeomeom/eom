using System;
using System.Collections;
using UnityEngine;

namespace PatchSystem
{
    public abstract class BaseOperation<OPERATION> : IEnumerator
        where OPERATION : BaseOperation<OPERATION>
    {
        public bool withoutError { get; private set; }
        public bool isDone { get; protected set; }
        public object Current { get { return null; } }
        public PatchSystemException exception { get; private set; }

        public OPERATION WithoutError()
        {
            this.withoutError = true;

            return this as OPERATION;
        }
        public bool MoveNext()
        {
            return false == isDone;
        }
        public void Reset()
        {

        }
        protected void OnException(PatchSystemException exceptionParam)
        {
            if (false == withoutError)
            {
                exception = exceptionParam;

                Debug.LogErrorFormat(exception.ToString());
            }
            
            isDone = true;
        }
        static protected void OnLog(string message, params object[] args)
        {
            if (PatchSystemManager.logMode == PatchSystemManager.LogMode.All)
                Debug.LogFormat(message, args);
        }
    }
}