using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace NCR.Engage.RoslynAnalysis.Test
{
    [TestClass]
    class ControllerActionMethodDetection : CodeFixVerifier
    {
        [TestMethod]
        public void NonPublicMethodsAreNotActionMethods()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        protected System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            return Request.CreateResponse(5.0);
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void StaticMethodsAreNotActionMethods()
        {
            string test = @"
namespace WebApp
{
    using System;
    using System.Net.Http;

    public class SomeController : System.Web.Http.ApiController
    {
        public static HttpRequestMessage Requestt { get; set; }

        public static System.Net.Http.HttpResponseMessage GetAsync(Guid id)
        {
            return Requestt.CreateResponse(5.0);
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
