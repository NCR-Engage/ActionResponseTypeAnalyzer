using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace NCR.Engage.RoslynAnalysis.Test
{
    [TestClass]
    public sealed class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void ThereIsNothingWrongAboutEmptyCode()
        {
            var test = @"";
            
            VerifyCSharpDiagnostic(test);
        }
        
        [TestMethod]
        public void ProperAnnotationsShouldNotThrowErrors()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        [System.Web.Http.Description.ResponseType(typeof(int))]
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            return Request.CreateResponse(5);
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ProperAnnotationsShouldNotThrowErrorsForExplicitTypeName()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        [System.Web.Http.Description.ResponseType(typeof(System.Int32))]
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            return Request.CreateResponse(5);
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void WrongAnnotationsShouldThrowError()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        [System.Web.Http.Description.ResponseType(typeof(int))]
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            return Request.CreateResponse(5.0);
        }
    }
}";

            var expected1 = new DiagnosticResult
            {
                Id = "ARTA001",
                Message = "Declared response type is 'int', but the actual response type is 'double'.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 10)
                        }
            };

            VerifyCSharpDiagnostic(test, expected1);
        }

        [TestMethod]
        public void ReportedGenericTypeNamesAreNicelyFormatted()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        [System.Web.Http.Description.ResponseType(typeof(List<int>))]
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            return Request.CreateResponse(5.0);
        }
    }
}";

            var expected1 = new DiagnosticResult
            {
                Id = "ARTA001",
                Message = "Declared response type is 'List<int>', but the actual response type is 'double'.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 10)
                        }
            };
            
            VerifyCSharpDiagnostic(test, expected1);
        }

        [TestMethod]
        public void NoAnnotationShouldThrowError()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            return Request.CreateResponse(5.0);
        }
    }
}";

            var expected1 = new DiagnosticResult
            {
                Id = "ARTA002",
                Message = "Response type is not specified in the ResponseType attribute.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 28)
                        }
            };

            VerifyCSharpDiagnostic(test, expected1);
        }

        [TestMethod]
        public void MultipleIssuesAreReported()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        [System.Web.Http.Description.ResponseType(typeof(int))]
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            if (Environment.NewLine == ""ciao"")
            {
                return Request.CreateResponse(""allo"");
            }

            return Request.CreateResponse(5.0);
        }
    }
}";

            var expected0 = new DiagnosticResult
            {
                Id = "ARTA001",
                Message = "Declared response type is 'int', but the actual response type is 'string'.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 10)
                }
            };

            var expected1 = new DiagnosticResult
            {
                Id = "ARTA001",
                Message = "Declared response type is 'int', but the actual response type is 'double'.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 10)
                        }
            };

            VerifyCSharpDiagnostic(test, new [] { expected0, expected1 });
        }

        [TestMethod]
        public void NongenericVersionsOfCreateResponseShouldBeIgnored()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            return Request.CreateResponse();
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NongenericVersionsOfCreateResponseShouldBeIgnoredEvenForStatusCodeCase()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            return Request.CreateResponse(System.Net.HttpStatusCode.BadRequest);
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ActionResponseTypeAnalyzer();
        }
    }
}