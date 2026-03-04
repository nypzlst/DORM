using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace DORM.Mapping.Roslyn
{
    [Generator]
    internal class OrmGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var temp = context.SyntaxProvider.ForAttributeWithMetadataName("DORM.TableAttribute",
                predicate : (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) =>
                {
                    var symbol = (INamedTypeSymbol)ctx.TargetSymbol;

                    var className = symbol.Name;

                    var propertys = symbol.GetMembers().OfType<IPropertySymbol>().ToDictionary(x=>x.Name,x=>x.Type.ToString());
                    return (className, propertys);
                });

            //TODO: generation code for sql query
        }
    }
}
