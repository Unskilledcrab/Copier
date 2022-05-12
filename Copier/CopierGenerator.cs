using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Copier
{
    [Generator]
    public class CopierGenerator : IIncrementalGenerator
    {
        private readonly static string _className = "Copier";
        private readonly static string _methodName = "Copy";
        string openText = $@"namespace System
{{
    public static partial class {_className}
    {{";
        string closeText = $@"    }}
}}
";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var copierTypes = context.SyntaxProvider
                        .CreateSyntaxProvider(CouldBeCopierAsync, GetCopierTypeOrNull)
                        .Where(type => type is not null)                 
                        .Collect();

            context.RegisterSourceOutput(copierTypes, GenerateCopier);
        }

        private bool CouldBeCopierAsync(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            // If it's not trying to invoke a method, do not bother
            if (syntaxNode is not InvocationExpressionSyntax expressionSyntax)
                return false;

            // If it is not trying to access a member of the class, do not bother
            var accessExpression = expressionSyntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (accessExpression is null)
                return false;

            // if it doesn't come from the right class, do not bother
            if (accessExpression.Expression.ToString() != _className)
                return false;

            // If it isn't calling the right method, do not bother
            if (!accessExpression.Name.Identifier.Text.StartsWith(_methodName))
                return false;

            // If we don't have 1 or two arguments, we can't doing mapping
            var argumentsListSyntax = expressionSyntax.DescendantNodes().OfType<ArgumentListSyntax>().FirstOrDefault();
            if (argumentsListSyntax is null || argumentsListSyntax.Arguments.Count > 2 || argumentsListSyntax.Arguments.Count == 0)
                return false;

            // If we don't have generic constraints we wont know what properties to map
            var genericNameSyntax = accessExpression.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault();
            if (genericNameSyntax is null)
                return false;

            var genericArgumentListSyntax = genericNameSyntax.DescendantNodes().OfType<TypeArgumentListSyntax>().FirstOrDefault();
            if (genericArgumentListSyntax is null)
                return false;

            // We must be mapping with at most two generics
            if (genericArgumentListSyntax.Arguments.Count > 2)
                return false;

            return true;
        }

        private CopyObject? GetCopierTypeOrNull(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            // We already know it's an invocation because it passed all validations above
            var expressionSyntax = (InvocationExpressionSyntax)context.Node;

            // Gather all of the information we need to generate the methods for these objects and put them into a containing object
            var potentialCopy = new CopyObject();
            var idBuilder = new StringBuilder();

            // Gather all of the generics in the method
            var accessExpression = expressionSyntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>().First();
            var genericNameSyntax = accessExpression.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault();

            if (genericNameSyntax is not null)
            {
                // TODO: need to look and see if there is a second generic here. if there is we are doing a single mapping
                // We are mapping to another type
                var genericArgumentSyntax = genericNameSyntax.DescendantNodes().OfType<TypeArgumentListSyntax>().First().Arguments[0];

                var typeInfo = context.SemanticModel.GetTypeInfo(genericArgumentSyntax, cancellationToken);
                if (typeInfo.Type is null || !typeInfo.Type.IsType)
                {
                    return null;
                }
                potentialCopy.Constraint = typeInfo;
                potentialCopy.Id = (typeInfo.Type?.Name);
            }

            // Gather all the argument types used
            var argumentListSyntax = expressionSyntax.ArgumentList.Arguments;


            potentialCopy.Arguments[0] = context.SemanticModel.GetTypeInfo(argumentListSyntax[0].ChildNodes().First(), cancellationToken);
            if (potentialCopy.Arguments[0].GetValueOrDefault().Type is null || !potentialCopy.Arguments[0].GetValueOrDefault().Type.IsType)
            {
                return null;
            }
            if (argumentListSyntax.Count > 1)
            {
                // We are mapping two objects, if count is only 1 then we are mapping a new object
                potentialCopy.Arguments[1] = context.SemanticModel.GetTypeInfo(argumentListSyntax[1].ChildNodes().First(), cancellationToken);
                if (potentialCopy.Arguments[1].GetValueOrDefault().Type is null || !potentialCopy.Arguments[1].GetValueOrDefault().Type.IsType)
                {
                    return null;
                }
            }

            // TODO: Check to make sure that the argument(s) implement the generic
            // TODO: If there are two generics, we need to make sure that the second generic implements new() and inherits from the first
            //if (potentialCopy.Arguments[0])
            //{

            //}

            //if (potentialCopy.Arguments[1] is not null)
            //{

            //}

            //potentialCopy.Id = idBuilder.ToString();
            return potentialCopy;
        }

        private void GenerateCopier(SourceProductionContext context, ImmutableArray<CopyObject?> copyObjects)
        {
            // TODO: Make sure that we are only making methods for distinct types
            var distinctCopyObjects = copyObjects.Distinct();
            //var distinctCopyObjects = copyObjects;

            // Parse all of the constraints to get all of the properties to copy
            var copyMethods = new List<CopyMethod>();
            foreach (var copyObject in distinctCopyObjects)
            {
                var copyMethod = new CopyMethod();
                ParseGenerics(copyObject, copyMethod);
                copyMethods.Add(copyMethod);
            }

            // Generate the source text for each method
            var sourceText = new StringBuilder();
            sourceText.AppendLine(openText);
            foreach (var copyMethod in copyMethods)
            {
                sourceText.AppendLine(GenerateMethod(copyMethod));
            }
            sourceText.Append(closeText);

            context.AddSource($"{_className}.g.cs", sourceText.ToString());
        }

        private static void ParseGenerics(CopyObject? copyObject, CopyMethod copyMethod)
        {
            if (copyObject is null)
            {
                return;
            }

            if (copyObject.Arguments[1] is not null)
            {
                copyMethod.Type = CopyType.CopyOver; // generate a method to copy properties to this argument
            }
            else
            {
                copyMethod.Type = CopyType.New; // We only have one argument so make a new copy of that type
            }

            // Get the first generic argument
            var constraintType = copyObject.Constraint.Type;
            var sourceType = copyObject.Arguments[0].GetValueOrDefault().Type;

            if (constraintType is null)
            {
                throw new ArgumentException("constraintType is null");
            }

            if (sourceType is null)
            {
                throw new ArgumentException("sourceType is null");
            }

            // Get the constraint type name
            copyMethod.Constraint = constraintType.Name;
            copyMethod.SourceType = sourceType.Name;

            // get all of the properties to map
            foreach (var property in constraintType.GetMembers()
                                                                .OfType<IPropertySymbol>()
                                                                .Where(p => p.SetMethod is not null 
                                                                    && p.GetMethod is not null 
                                                                    && p.DeclaredAccessibility == Accessibility.Public))
            {
                copyMethod.PropertyNames.Add(property.Name);
            }
        }

        private static string GenerateMethod(CopyMethod copyMethod)
        {
            var methodText = string.Empty;
            switch (copyMethod.Type)
            {
                case CopyType.CopyOver:
                    methodText = GetCopyOverMethod(copyMethod);
                    break;
                case CopyType.New:
                    methodText = GetCopyNewMethod(copyMethod);
                    break;
                default:
                    break;
            }

            return methodText;
        }

        private static string GetCopyOverMethod(CopyMethod copyMethod)
        {
            return $@"        public static void {_methodName}<TConstraint>({copyMethod.Constraint} source, {copyMethod.Constraint} target) where TConstraint : {copyMethod.Constraint}
        {{
            {GetPropertyMappings(copyMethod)}
        }}";
        }

        private static string GetCopyNewMethod(CopyMethod copyMethod)
        {
            return $@"        public static {copyMethod.Constraint} {_methodName}<TConstraint>({copyMethod.SourceType} source) where TConstraint : {copyMethod.Constraint}, new()
        {{
            var target = new {copyMethod.Constraint}();
            {GetPropertyMappings(copyMethod)}
            return target;
        }}";
        }

        private static string GetPropertyMappings(CopyMethod copyMethod)
        {
            return string.Join($"{Environment.NewLine}            ", copyMethod.PropertyNames.Select(p => $"target.{p} = source.{p};"));
        }
    }

    public enum CopyType
    {
        New,
        CopyOver,
    }

    public sealed class CopyMethod : IEqualityComparer<CopyObject>
    {
        public CopyType Type { get; set; } = CopyType.New;
        public List<string> PropertyNames { get; set; } = new();
        public string SourceType { get; set; } = "";
        public string Constraint { get; set; } = ""; 
        
        public bool Equals(CopyObject x, CopyObject y)
        {
            return string.Equals(x.Id, y.Id);
        }

        public int GetHashCode(CopyObject obj)
        {
            return obj.Id?.GetHashCode() ?? 0;
        }
    }

    public sealed class CopyObject
    {
        public string? Id { get; set; } = null;
        public TypeInfo Constraint { get; set; }
        public TypeInfo?[] Arguments { get; set; } = new TypeInfo?[2];
    }
}
