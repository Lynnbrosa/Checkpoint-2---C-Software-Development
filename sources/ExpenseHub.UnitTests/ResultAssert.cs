using ExpenseHub.Api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

internal static class ResultAssert
{
    public static T Succeeded<T>(ServiceResult<T> result)
        where T : notnull
    {
        Assert.IsTrue(result.Succeeded, result.Error?.Message);
        Assert.IsNotNull(result.Value);
        return result.Value;
    }

    public static ServiceError Failed<T>(ServiceResult<T> result, ServiceErrorKind expected)
        where T : notnull
    {
        Assert.IsFalse(result.Succeeded, "era esperado erro, mas a operação passou");
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(expected, result.Error.Kind, result.Error.Message);
        return result.Error;
    }
}
