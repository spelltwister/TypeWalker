using System;
using System.Reflection;

namespace TypeWalker
{
    public class MethodEventArgs : EventArgs
    {
        public string MethodName { get; set; }

        public MethodInfo MethodInfo { get; set; }

        public bool IsOwnMethod { get; set; }
    }
}