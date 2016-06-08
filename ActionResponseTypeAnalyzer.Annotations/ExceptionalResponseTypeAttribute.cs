using System;

namespace NCR.Engage.RoslynAnalysis
{
    /// <summary>
    /// This attribute marks class whose instances
    /// are allowed to pass ActionResponseTypeAnalyzer
    /// checks even when they differ from stated type.
    /// </summary>
    public class ExceptionalResponseTypeAttribute : Attribute
    {
    }
}
