using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace DORM.Mapping.Roslyn
{
    internal class TableRoslyn
    {

        

        public void CreateTable<T> () where T : class
        {
            string code = File.ReadAllText("MyClass.cs");
            var tree = CSharpSyntaxTree.ParseText(code);
        }
    }
}
