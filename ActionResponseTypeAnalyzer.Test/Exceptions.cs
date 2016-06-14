using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace NCR.Engage.RoslynAnalysis.Test
{
    [TestClass]
    public class Exceptions : CodeFixVerifier
    {
        [TestMethod]
        public void MarkedResultsAreExcludedFromReportingWhenThereIsNoOtherError()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;
    using NCR.Engage.RoslynAnalysis;

    public class SomeController : System.Web.Http.ApiController
    {
        [System.Web.Http.Description.ResponseType(typeof(double))]
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            if (Environment.NewLine == ""\r\n\r"")
            {
                return Request.CreateResponse(new ErrorState{ Message = ""An exceptional state arised. Just lettin' you know."" });
            }

            return Request.CreateResponse(5.0);
        }
    }

    [ExceptionalResponseType]
    public class ErrorState
    {
        public string Message { get; set; }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void MarkedResultsAreExcludedFromReportingWhenThereIsOtherError()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;
    using NCR.Engage.RoslynAnalysis;

    public class SomeController : System.Web.Http.ApiController
    {
        [System.Web.Http.Description.ResponseType(typeof(int))]
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            if (Environment.NewLine == ""\r\n\r"")
            {
                return Request.CreateResponse(new ErrorState{ Message = ""An exceptional state arised. Just lettin' you know."" });
            }

            return Request.CreateResponse(5.0);
        }
    }

    [ExceptionalResponseType]
    public class ErrorState
    {
        public string Message { get; set; }
    }
}";

            var expected1 = new DiagnosticResult
            {
                Id = "ARTA001",
                Message = "Declared response type is 'int', but the actual response type is 'double'.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 10)
                        }
            };

            VerifyCSharpDiagnostic(test, expected1);
        }

        [TestMethod]
        public void MarkedResultsAreExcludedFromReportingEvenForMissingResponseTypeError()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;
    using NCR.Engage.RoslynAnalysis;

    public class SomeController : System.Web.Http.ApiController
    {
        public System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            if (Environment.NewLine == ""\r\n\r"")
            {
                return Request.CreateResponse(new ErrorState{ Message = ""An exceptional state arised. Just lettin' you know."" });
            }

            return Request.CreateResponse(new ErrorState{ Message = ""Some other error."" });
        }
    }

    [ExceptionalResponseType]
    public class ErrorState
    {
        public string Message { get; set; }
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
