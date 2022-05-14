using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Copier
{
    public static class CopierDiagnostic
    {
        public static Diagnostic WrongConstraints(InvocationExpressionSyntax syntaxNode)
        {
            var descriptor = new DiagnosticDescriptor(
            id: "COPIER01",
            title: "A test diagnostic",
            messageFormat: "A description about the problem",
            category: "Constraints",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

            return Diagnostic.Create(descriptor, syntaxNode.GetLocation());
        }
    }

    [Generator]
    public class CopierGenerator : IIncrementalGenerator
    {
        private readonly static string _className = "Copier";
        private readonly static string _methodName = "Copy";
        string openText = $@"namespace System
{{
    public partial class {_className}
    {{";
        string closeText = $@"    }}
}}
";

        private HashSet<string> _referenceProperties = new HashSet<string>();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var copierTypes = context.SyntaxProvider
                        .CreateSyntaxProvider(CouldBeCopierAsync, GetCopierTypeOrNull)
                        .Where(type => type is not null)
                        .Collect();

            context.RegisterSourceOutput(copierTypes, GenerateCopier);
        }

        private void GenerateCopier(SourceProductionContext arg1, ImmutableArray<CopyObject> arg2)
        {
            _referenceProperties.Clear();
            foreach (var copyObject in arg2.Distinct())
            {
                GenerateCopier(arg1, copyObject);
            }
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

            // Gather all of the generics in the method
            var accessExpression = expressionSyntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>().First();
            var genericNameSyntax = accessExpression.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault();

            if (genericNameSyntax is not null)
            {
                // TODO: need to look and see if there is a second generic here. if there is we are doing a single mapping
                // We are mapping to another type
                var genericArgumentSyntax = genericNameSyntax.DescendantNodes().OfType<TypeArgumentListSyntax>().First().Arguments[0];

                var typeInfo = context.SemanticModel.GetTypeInfo(genericArgumentSyntax, cancellationToken).Type;
                if (typeInfo is null || !typeInfo.IsType)
                {
                    return null;
                }
                potentialCopy.Constraint = typeInfo;
            }

            // Gather all the argument types used
            var argumentListSyntax = expressionSyntax.ArgumentList.Arguments;


            var isMapping = false;
            potentialCopy.Arguments[0] = context.SemanticModel.GetTypeInfo(argumentListSyntax[0].ChildNodes().First(), cancellationToken).Type;
            var firstArgumentType = potentialCopy.Arguments[0];
            if (firstArgumentType is null || !firstArgumentType.IsType)
            {
                return null;
            }
            if (argumentListSyntax.Count > 1)
            {
                // We are mapping two objects, if count is only 1 then we are mapping a new object
                potentialCopy.Arguments[1] = context.SemanticModel.GetTypeInfo(argumentListSyntax[1].ChildNodes().First(), cancellationToken).Type;
                var secondArgumentType = potentialCopy.Arguments[1];
                if (secondArgumentType is null || !secondArgumentType.IsType)
                {
                    return null;
                }
                else
                {
                    isMapping = true;
                }
            }

            // TODO: Check to make sure that the argument(s) implement the generic
            // TODO: If there are two generics, we need to make sure that the second generic implements new() and inherits from the first
            //if (isMapping)
            //{
            // If we are mapping, make sure the that both arguments implement the Constraint
            //}
            //else
            //{
            // If we are creating new, make sure the TConstaint implements new() and that the first arguement implements the constraint
            //}

            //if (potentialCopy.Arguments[1] is not null)
            //{

            //}

            return potentialCopy;
        }


        private void GenerateCopier(SourceProductionContext context, CopyObject copyObject)
        {
            if (_referenceProperties.Contains(copyObject.Constraint.Name) && _referenceProperties.Contains(copyObject.Arguments[0].Name))
            {
                return;
            }

            if (copyObject.Constraint.Name == copyObject.Arguments[0].Name && copyObject.Arguments[1] is null)
            {
                _referenceProperties.Add(copyObject.Constraint.Name);
            }

            var copyMethod = ParseGenerics(context, copyObject);

            // Generate the source text for each method
            var sourceText = new StringBuilder();
            sourceText.AppendLine(openText);
            sourceText.AppendLine(GenerateMethod(copyMethod));
            sourceText.Append(closeText);

            context.AddSource($"{copyObject.Constraint.Name}_{copyObject.Arguments[0]}.g.cs", sourceText.ToString());
        }

        private CopyMethod ParseGenerics(SourceProductionContext context, CopyObject copyObject)
        {
            var copyMethod = new CopyMethod();

            if (copyObject.Arguments[1] is not null)
            {
                copyMethod.Type = CopyType.CopyOver; // generate a method to copy properties to this argument
            }
            else
            {
                copyMethod.Type = CopyType.New; // We only have one argument so make a new copy of that type
            }

            // Get the first generic argument
            var constraintType = copyObject.Constraint;
            var sourceType = copyObject.Arguments[0];

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
                if (property.Type.IsReferenceType && property.Type.Name != "String")
                {
                    copyMethod.ReferencePropertyNames.Add((property.Name, property.Type.Name));

                    // Recursively call generate copier so that the reference copier is added
                    var propertyCopyObject = new CopyObject();
                    propertyCopyObject.Constraint = property.Type;
                    propertyCopyObject.Arguments[0] = property.Type;
                    GenerateCopier(context, propertyCopyObject);
                }
                else if (property.Type.IsValueType || property.Type.Name == "String")
                {
                    copyMethod.ValuePropertyNames.Add(property.Name);
                }
            }
            return copyMethod;
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
            {GetPropertyMappings(copyMethod)}{GetReferencePropertyMappings(copyMethod)}
        }}";
        }

        private static string GetCopyNewMethod(CopyMethod copyMethod)
        {
            return $@"        public static {copyMethod.Constraint} {_methodName}<TConstraint>({copyMethod.SourceType} source) where TConstraint : {copyMethod.Constraint}, new()
        {{
            if (source == null) return source;
            var target = new {copyMethod.Constraint}();
            {GetPropertyMappings(copyMethod)}{GetReferencePropertyMappings(copyMethod)}{GetSelfReferencePropertyMappings(copyMethod)}
            return target;
        }}";
        }

        private static string GetPropertyMappings(CopyMethod copyMethod)
        {
            return string.Join($"{Environment.NewLine}            ", copyMethod.ValuePropertyNames.Select(p => $"target.{p} = source.{p};"));
        }

        private static string GetReferencePropertyMappings(CopyMethod copyMethod)
        {
            return string.Join($"{Environment.NewLine}            ", copyMethod.ReferencePropertyNames.Where(r => r.type != copyMethod.SourceType).Select(p => $"if (!ReferenceEquals(source.{p.propertyName}, null)) target.{p.propertyName} = {_className}.{_methodName}<{p.type}>(source.{p.propertyName});"));
        }

        private static string GetSelfReferencePropertyMappings(CopyMethod copyMethod)
        {
            return string.Join($"{Environment.NewLine}            ", copyMethod.ReferencePropertyNames.Where(r => r.type == copyMethod.SourceType).Select(p => $"if (!ReferenceEquals(source.{p.propertyName}, null) && !ReferenceEquals(source.{p.propertyName}, source)) {{ target.{p.propertyName} = {_className}.{_methodName}<{p.type}>(source.{p.propertyName}); }} else {{ target.{p.propertyName} = source.{p.propertyName}; }}"));
        }
    }

    public enum CopyType
    {
        New,
        CopyOver,
    }

    public sealed class CopyMethod
    {
        public CopyType Type { get; set; } = CopyType.New;
        public List<string> ValuePropertyNames { get; set; } = new();
        public List<(string propertyName, string type)> ReferencePropertyNames { get; set; } = new();
        public string SourceType { get; set; } = "";
        public string Constraint { get; set; } = "";

    }

    public sealed class CopyObject : IEquatable<CopyObject>
    {
        public ITypeSymbol Constraint { get; set; }
        public ITypeSymbol?[] Arguments { get; set; } = new ITypeSymbol?[2];

        public bool Equals(CopyObject other)
        {
            //Check whether the compared object is null.
            if (ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data.
            if (ReferenceEquals(this, other)) return true;

            //Check whether the CopyObject properties are equal.
            return
                string.Equals(Constraint?.Name, other.Constraint?.Name) &&
                string.Equals(Arguments[0]?.Name, other.Arguments[0]?.Name) &&
                string.Equals(Arguments[1]?.Name, other.Arguments[1]?.Name);
        }

        public override int GetHashCode()
        {
            var constraint = Constraint?.Name?.GetHashCode() ?? 0;
            var arg1 = Arguments[0]?.Name.GetHashCode() ?? 0;
            var arg2 = Arguments[1]?.Name.GetHashCode() ?? 0;
            return constraint ^ arg1 ^ arg2;
        }
    }
}
