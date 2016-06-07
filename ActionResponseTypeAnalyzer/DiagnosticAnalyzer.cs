using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NCR.Engage.RoslynAnalysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ActionResponseTypeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor TypeMismatchIssue = new DiagnosticDescriptor(
            "ActionResponseTypeAnalyzerTypeMismatchIssue",
            "Value type declared in ResponseType should match to actual response type.",
            "Declared response type is '{0}', but the actual response type is '{1}'.",
            "Naming",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor AttributeMissingIssue = new DiagnosticDescriptor(
            "ActionResponseTypeAnalyzerAttributeMissingIssue",
            "Public controller method specify their response type in the ResponseType attribute.",
            "Response type is not specified in the ResponseType attribute.",
            "Naming",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(TypeMismatchIssue, AttributeMissingIssue);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeCreateResponseCall, SyntaxKind.IdentifierName);
        }

        private static void AnalyzeCreateResponseCall(SyntaxNodeAnalysisContext context)
        {
            // Are we looking at CreateResponse call?
            // Let's do a quick, syntactic check.
            var identifierNameSyntax = context.Node as IdentifierNameSyntax;
            if (identifierNameSyntax?.Identifier.ToString() != "CreateResponse")
            {
                return;
            }

            // It seems like we do. Let's make ourselves sure by
            // proceeding with slow, semantic check.
            var symbol = context.SemanticModel.GetSymbolInfo(identifierNameSyntax).Symbol as IMethodSymbol;
            if (symbol == null ||
                !symbol.ToString().StartsWith("System.Net.Http.HttpRequestMessage.CreateResponse"))
            {
                return;
            }
            
            // The type argument of CreateResponse is the actual response
            // type that may or may not be same as not-yet-discovered
            // declared type.
            var actualResponseType = symbol.TypeArguments.First();


            var currentMethod = GetCurrentMethod(identifierNameSyntax);
            if (currentMethod == null)
            {
                return;
            }

            var currentMethodSymbol = context.SemanticModel.GetDeclaredSymbol(currentMethod);
            if (currentMethodSymbol.IsStatic || currentMethodSymbol.DeclaredAccessibility != Accessibility.Public)
            {
                return;
            }
            
            var attributesInSemantics = GetAttributes(currentMethod, context.SemanticModel, "System.Web.Http.Description.ResponseTypeAttribute");

            if (attributesInSemantics.Count != 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(AttributeMissingIssue, identifierNameSyntax.GetLocation()));
                return;
            }

            var theAttribute = attributesInSemantics.First();

            var declaredType = GetResponseTypeType(theAttribute.Item1, context.SemanticModel);

            if (declaredType == null)
            {
                return;
            }

            if (declaredType.Name != actualResponseType?.Name)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    TypeMismatchIssue,
                    theAttribute.Item1.GetLocation(),
                    declaredType.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                    actualResponseType?.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            }
        }

        private static MethodDeclarationSyntax GetCurrentMethod(IdentifierNameSyntax identifierName)
        {
            var sn = (identifierName as SyntaxNode);

            while (!(sn is MethodDeclarationSyntax))
            {
                sn = sn.Parent;

                if (sn == null)
                {
                    return null;
                }
            }

            return sn as MethodDeclarationSyntax;
        }
        
        private static IList<Tuple<AttributeSyntax, IMethodSymbol>> GetAttributes(MethodDeclarationSyntax currentMethod, SemanticModel semanticModel, string attributeFullName)
        {
            var attributeName = attributeFullName.Split('.').Last();
            var attributeNameWithoutAttributeSuffix = attributeName.Substring(0, attributeName.Length - "Attribute".Length);

            var possibleAttributesInSyntax = currentMethod.ChildNodes()
                .Where(chn => chn is AttributeListSyntax)
                .Cast<AttributeListSyntax>()
                .Where(chn => chn.ChildNodes().Any(chchn => chchn is AttributeSyntax))
                .Select(chn => (AttributeSyntax) chn.ChildNodes().First(chchn => chchn is AttributeSyntax))
                .Where(chchn => chchn.Name.ToString().EndsWith(attributeNameWithoutAttributeSuffix) || chchn.Name.ToString().EndsWith(attributeName));

            return possibleAttributesInSyntax
                .Select(a => Tuple.Create(a, semanticModel.GetSymbolInfo(a).Symbol as IMethodSymbol))
                .Where(s => s.Item2.ToString().StartsWith(attributeFullName))
                .ToList();
        }

        private static ITypeSymbol GetResponseTypeType(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var typeofDeclaration = attribute
                .GoDown(typeof (AttributeArgumentListSyntax))
                .GoDown(typeof (AttributeArgumentSyntax))
                .GoDown(typeof (TypeOfExpressionSyntax));

            var symbols = typeofDeclaration.ChildNodes().Select(n => semanticModel.GetSymbolInfo(n).Symbol as ITypeSymbol);

            return symbols.FirstOrDefault(symbol => symbol != null);
        }
    }

    public static class RoslynExtensions
    {
        public static SyntaxNode GoDown(this SyntaxNode n, Type t)
        {
            return n?.ChildNodes().FirstOrDefault(sn => sn.GetType() == t);
        }
    }
}
