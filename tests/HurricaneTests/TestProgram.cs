// -----------------------------------------------------------------------
// <copyright file="Main.cs" company="Jiangyan Xu">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace GSeries {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Reflection;

    public class TestProgram {
        public static void Main(string[] args) {
            string startupClass = args[0];
            string methodToCall = args[1];
            var argl = new List<string>(args);
            argl.RemoveAt(0);
            argl.RemoveAt(0);
            var type = Type.GetType(startupClass);
            var method = type.GetMethod(methodToCall, BindingFlags.Static | BindingFlags.Public);
            method.Invoke(type, argl.ToArray());
        }
    }
}
